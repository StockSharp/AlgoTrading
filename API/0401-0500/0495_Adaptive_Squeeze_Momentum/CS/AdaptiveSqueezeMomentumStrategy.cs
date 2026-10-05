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
/// Trades only when the squeeze is released (Bollinger Bands outside the Keltner Channel) and ATR is at least MinVolatility percent
/// of price. A long needs momentum above MomentumMultiplier standard deviations of momentum, RSI above RsiOversold and a
/// rising trend EMA; a short mirrors this with RSI below RsiOverbought and a falling EMA (both filters optional). The
/// opposite signal reverses; ATR stop-loss and take-profit levels and a holding period of HoldingPeriodMultiplier * MomentumLength
/// bars close positions.
/// </summary>
public class AdaptiveSqueezeMomentumStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _keltnerPeriod;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _trendMaLength;
	private readonly StrategyParam<bool> _useAtrStops;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _minVolatility;
	private readonly StrategyParam<decimal> _holdingPeriodMultiplier;
	private readonly StrategyParam<bool> _useTrendFilter;
	private readonly StrategyParam<bool> _useRsiFilter;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _momentumMultiplier;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _momentumStdDev;
	private decimal? _prevEma;
	private decimal? _stopPrice;
	private decimal? _takePrice;
	private int _barsInPosition;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands standard deviation multiplier.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Keltner Channel period.
	/// </summary>
	public int KeltnerPeriod
	{
		get => _keltnerPeriod.Value;
		set => _keltnerPeriod.Value = value;
	}

	/// <summary>
	/// Keltner Channel ATR multiplier.
	/// </summary>
	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	/// <summary>
	/// Momentum period, also the window of its standard deviation.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// Period of the trend EMA.
	/// </summary>
	public int TrendMaLength
	{
		get => _trendMaLength.Value;
		set => _trendMaLength.Value = value;
	}

	/// <summary>
	/// Use ATR stop-loss and take-profit.
	/// </summary>
	public bool UseAtrStops
	{
		get => _useAtrStops.Value;
		set => _useAtrStops.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the stop-loss.
	/// </summary>
	public decimal AtrMultiplierSl
	{
		get => _atrMultiplierSl.Value;
		set => _atrMultiplierSl.Value = value;
	}

	/// <summary>
	/// ATR multiplier of the take-profit.
	/// </summary>
	public decimal AtrMultiplierTp
	{
		get => _atrMultiplierTp.Value;
		set => _atrMultiplierTp.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// Minimum ATR as a percent of the close required to trade.
	/// </summary>
	public decimal MinVolatility
	{
		get => _minVolatility.Value;
		set => _minVolatility.Value = value;
	}

	/// <summary>
	/// Holding period in multiples of MomentumLength bars.
	/// </summary>
	public decimal HoldingPeriodMultiplier
	{
		get => _holdingPeriodMultiplier.Value;
		set => _holdingPeriodMultiplier.Value = value;
	}

	/// <summary>
	/// Require the trend EMA to slope in the trade direction.
	/// </summary>
	public bool UseTrendFilter
	{
		get => _useTrendFilter.Value;
		set => _useTrendFilter.Value = value;
	}

	/// <summary>
	/// Require RSI on the trade side of its level.
	/// </summary>
	public bool UseRsiFilter
	{
		get => _useRsiFilter.Value;
		set => _useRsiFilter.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI level a long must be above.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// RSI level a short must be below.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Standard deviations of momentum that make the dynamic threshold.
	/// </summary>
	public decimal MomentumMultiplier
	{
		get => _momentumMultiplier.Value;
		set => _momentumMultiplier.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public AdaptiveSqueezeMomentumStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands period", "Squeeze");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Bollinger Bands standard deviation multiplier", "Squeeze");

		_keltnerPeriod = Param(nameof(KeltnerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Period", "Keltner Channel period", "Squeeze");

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Multiplier", "Keltner Channel ATR multiplier", "Squeeze");

		_momentumLength = Param(nameof(MomentumLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Momentum period, also the window of its standard deviation", "Momentum");

		_trendMaLength = Param(nameof(TrendMaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend MA Length", "Period of the trend EMA", "Filters");

		_useAtrStops = Param(nameof(UseAtrStops), true)
			.SetDisplay("Use ATR Stops", "Use ATR stop-loss and take-profit", "Risk");

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1.5m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier SL", "ATR multiplier of the stop-loss", "Risk");

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 2.5m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier TP", "ATR multiplier of the take-profit", "Risk");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Risk");

		_minVolatility = Param(nameof(MinVolatility), 0.5m)
			.SetNotNegative()
			.SetDisplay("Min Volatility %", "Minimum ATR as a percent of the close required to trade", "Filters");

		_holdingPeriodMultiplier = Param(nameof(HoldingPeriodMultiplier), 1.5m)
			.SetNotNegative()
			.SetDisplay("Holding Period Multiplier", "Holding period in multiples of MomentumLength bars", "Risk");

		_useTrendFilter = Param(nameof(UseTrendFilter), true)
			.SetDisplay("Use Trend Filter", "Require the trend EMA to slope in the trade direction", "Filters");

		_useRsiFilter = Param(nameof(UseRsiFilter), true)
			.SetDisplay("Use RSI Filter", "Require RSI on the trade side of its level", "Filters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Filters");

		_rsiOversold = Param(nameof(RsiOversold), 40m)
			.SetDisplay("RSI Oversold", "RSI level a long must be above", "Filters");

		_rsiOverbought = Param(nameof(RsiOverbought), 60m)
			.SetDisplay("RSI Overbought", "RSI level a short must be below", "Filters");

		_momentumMultiplier = Param(nameof(MomentumMultiplier), 1.5m)
			.SetNotNegative()
			.SetDisplay("Momentum Multiplier", "Standard deviations of momentum that make the dynamic threshold", "Momentum");

		_allowLong = Param(nameof(AllowLong), true)
			.SetDisplay("Allow Long", "Allow long trades", "General");

		_allowShort = Param(nameof(AllowShort), true)
			.SetDisplay("Allow Short", "Allow short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_momentumStdDev = null;
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var bollinger = new BollingerBands { Length = BollingerPeriod, Width = BollingerMultiplier };
		var keltner = new KeltnerChannels { Length = KeltnerPeriod, Multiplier = KeltnerMultiplier };
		var momentum = new Momentum { Length = MomentumLength };
		var trendEma = new ExponentialMovingAverage { Length = TrendMaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		_momentumStdDev = new StandardDeviation { Length = MomentumLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx([bollinger, keltner, momentum, trendEma, atr, rsi], ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, keltner);
			DrawIndicator(area, trendEma);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_prevEma = null;
		_stopPrice = null;
		_takePrice = null;
		_barsInPosition = 0;
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var momentumValue = values[2];
		decimal? threshold = null;
		if (momentumValue.IsFormed)
		{
			var stdDev = _momentumStdDev.Process(momentumValue.ToDecimal(), candle.ServerTime, true);
			if (_momentumStdDev.IsFormed)
				threshold = stdDev.ToDecimal() * MomentumMultiplier;
		}

		foreach (var value in values)
		{
			if (!value.IsFormed)
				return;
		}

		if (values[0] is not BollingerBandsValue { UpBand: decimal bbUpper, LowBand: decimal bbLower }
			|| values[1] is not KeltnerChannelsValue { Upper: decimal kcUpper, Lower: decimal kcLower })
			return;

		var momentum = momentumValue.ToDecimal();
		var ema = values[3].ToDecimal();
		var atr = values[4].ToDecimal();
		var rsi = values[5].ToDecimal();

		var prevEma = _prevEma;
		_prevEma = ema;

		if (threshold is not decimal momentumThreshold || prevEma is not decimal pEma)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (Position != 0 && ManagePosition(candle))
			return;

		var squeezeReleased = bbUpper > kcUpper && bbLower < kcLower;
		var volatile_ = close > 0 && atr / close * 100m >= MinVolatility;

		var longSignal = AllowLong && squeezeReleased && volatile_ && momentum > momentumThreshold
			&& (!UseRsiFilter || rsi > RsiOversold)
			&& (!UseTrendFilter || ema > pEma);

		var shortSignal = AllowShort && squeezeReleased && volatile_ && momentum < -momentumThreshold
			&& (!UseRsiFilter || rsi < RsiOverbought)
			&& (!UseTrendFilter || ema < pEma);

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			SetLevels(close, atr, true);
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			SetLevels(close, atr, false);
		}
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		_barsInPosition++;

		var hitStop = Position > 0
			? (_stopPrice is decimal ls && candle.LowPrice <= ls) || (_takePrice is decimal lt && candle.HighPrice >= lt)
			: (_stopPrice is decimal ss && candle.HighPrice >= ss) || (_takePrice is decimal st && candle.LowPrice <= st);

		var holdingBars = (int)Math.Round(MomentumLength * HoldingPeriodMultiplier);
		var expired = holdingBars > 0 && _barsInPosition >= holdingBars;

		if (!hitStop && !expired)
			return false;

		if (Position > 0)
			SellMarket(Position);
		else
			BuyMarket(-Position);

		_stopPrice = null;
		_takePrice = null;
		_barsInPosition = 0;
		return true;
	}

	private void SetLevels(decimal price, decimal atr, bool isLong)
	{
		_barsInPosition = 0;

		if (!UseAtrStops)
		{
			_stopPrice = null;
			_takePrice = null;
			return;
		}

		var stop = atr * AtrMultiplierSl;
		var take = atr * AtrMultiplierTp;
		_stopPrice = stop > 0 ? (isLong ? price - stop : price + stop) : null;
		_takePrice = take > 0 ? (isLong ? price + take : price - take) : null;
	}
}
