using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Adaptive Squeeze Momentum strategy.
/// Detects squeeze release with Bollinger Bands and Keltner Channels
/// and confirms breakout using momentum.
/// </summary>
public class AdaptiveSqueezeMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _trendMaLength;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private bool _squeezeOffPrev;
	private decimal _stopPrice;
	private decimal _profitTarget;
	private int _cooldownRemaining;

	public int BollingerPeriod { get => _bollingerPeriod.Value; set => _bollingerPeriod.Value = value; }
	public decimal BollingerMultiplier { get => _bollingerMultiplier.Value; set => _bollingerMultiplier.Value = value; }
	public int KeltnerPeriod { get => _keltnerPeriod.Value; set => _keltnerPeriod.Value = value; }
	public decimal KeltnerMultiplier { get => _keltnerMultiplier.Value; set => _keltnerMultiplier.Value = value; }
	public int MomentumLength { get => _momentumLength.Value; set => _momentumLength.Value = value; }
	public int TrendMaLength { get => _trendMaLength.Value; set => _trendMaLength.Value = value; }
	public decimal AtrMultiplierSl { get => _atrMultiplierSl.Value; set => _atrMultiplierSl.Value = value; }
	public decimal AtrMultiplierTp { get => _atrMultiplierTp.Value; set => _atrMultiplierTp.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public AdaptiveSqueezeMomentumStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Periods for Bollinger Bands", "Indicators");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2.0m)
			.SetDisplay("Bollinger Multiplier", "Deviation multiplier", "Indicators");

		_keltnerPeriod = Param(nameof(KeltnerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Period", "EMA period for Keltner Channels", "Indicators");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Multiplier", "ATR multiplier for Keltner Channels", "Indicators");

		_momentumLength = Param(nameof(MomentumLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Periods for momentum", "Indicators");

		_trendMaLength = Param(nameof(TrendMaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend EMA Length", "EMA period for trend filter", "Indicators");

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Mult", "ATR multiplier for stop-loss", "Risk");

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Take Mult", "ATR multiplier for take-profit", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "Period for ATR", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_squeezeOffPrev = false;
		_stopPrice = 0;
		_profitTarget = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerMultiplier };
		var keltner = new KeltnerChannels { Length = KeltnerPeriod, Multiplier = KeltnerMultiplier };
		var momentum = new Momentum { Length = MomentumLength };
		var trendEma = new ExponentialMovingAverage { Length = TrendMaLength };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(bollinger, keltner, momentum, trendEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue keltnerValue, IIndicatorValue momentumValue, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (bollingerValue.IsEmpty || keltnerValue.IsEmpty || momentumValue.IsEmpty || emaValue.IsEmpty || atrValue.IsEmpty)
			return;

		var bb = (BollingerBandsValue)bollingerValue;
		var kc = (KeltnerChannelsValue)keltnerValue;

		if (bb.UpBand is not decimal bbUpper || bb.LowBand is not decimal bbLower ||
			kc.Upper is not decimal kcUpper || kc.Lower is not decimal kcLower)
			return;

		var mom = momentumValue.ToDecimal();
		var trend = emaValue.ToDecimal();
		var atrVal = atrValue.ToDecimal();

		// Squeeze: BB inside KC
		var squeezeOff = bbLower < kcLower && bbUpper > kcUpper;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			// Still check stops
			CheckStops(candle, atrVal);
			_squeezeOffPrev = squeezeOff;
			return;
		}

		// Check stops first
		if (CheckStops(candle, atrVal))
		{
			_squeezeOffPrev = squeezeOff;
			return;
		}

		var bullishTrend = candle.ClosePrice > trend;
		var bearishTrend = candle.ClosePrice < trend;

		var buySignal = _squeezeOffPrev && mom > 0 && bullishTrend;
		var sellSignal = _squeezeOffPrev && mom < 0 && bearishTrend;

		_squeezeOffPrev = squeezeOff;

		if (buySignal && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_stopPrice = candle.ClosePrice - atrVal * AtrMultiplierSl;
			_profitTarget = candle.ClosePrice + atrVal * AtrMultiplierTp;
			_cooldownRemaining = CooldownBars;
		}
		else if (sellSignal && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_stopPrice = candle.ClosePrice + atrVal * AtrMultiplierSl;
			_profitTarget = candle.ClosePrice - atrVal * AtrMultiplierTp;
			_cooldownRemaining = CooldownBars;
		}
	}

	private bool CheckStops(ICandleMessage candle, decimal atrVal)
	{
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _profitTarget)
			{
				SellMarket(Math.Abs(Position));
				_cooldownRemaining = CooldownBars;
				_stopPrice = 0;
				return true;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.ClosePrice >= _stopPrice || candle.ClosePrice <= _profitTarget)
			{
				BuyMarket(Math.Abs(Position));
				_cooldownRemaining = CooldownBars;
				_stopPrice = 0;
				return true;
			}
		}
		return false;
	}
}
