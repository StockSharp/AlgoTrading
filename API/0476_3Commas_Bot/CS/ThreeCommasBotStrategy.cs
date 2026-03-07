namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Simplified port of the "3Commas Bot" TradingView strategy.
/// Uses two moving averages for trend detection.
/// Enters long on bullish cross, short on bearish cross.
/// Exits via opposite cross or ATR-based stop-loss.
/// </summary>
public class ThreeCommasBotStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength1;
	private readonly StrategyParam<int> _maLength2;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _riskM;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _stopPrice;
	private decimal _entryPrice;
	private bool _initialized;
	private bool _wasFastAboveSlow;
	private int _cooldownRemaining;

	public int MaLength1 { get => _maLength1.Value; set => _maLength1.Value = value; }
	public int MaLength2 { get => _maLength2.Value; set => _maLength2.Value = value; }
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	public decimal RiskM { get => _riskM.Value; set => _riskM.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	public ThreeCommasBotStrategy()
	{
		_maLength1 = Param(nameof(MaLength1), 50)
			.SetDisplay("MA Length #1", "Fast moving average length", "MA Settings")
			.SetGreaterThanZero();

		_maLength2 = Param(nameof(MaLength2), 100)
			.SetDisplay("MA Length #2", "Slow moving average length", "MA Settings")
			.SetGreaterThanZero();

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR length", "ATR calculation period", "Risk Management")
			.SetGreaterThanZero();

		_riskM = Param(nameof(RiskM), 3m)
			.SetDisplay("Risk Adjustment", "ATR multiplier for stop", "Risk Management")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 15)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_stopPrice = 0;
		_entryPrice = 0;
		_initialized = false;
		_wasFastAboveSlow = false;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new ExponentialMovingAverage { Length = MaLength1 };
		var slowMa = new ExponentialMovingAverage { Length = MaLength2 };
		var atr = new AverageTrueRange { Length = AtrLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastMa, slowMa, atr, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_initialized)
		{
			_wasFastAboveSlow = fastValue > slowValue;
			_initialized = true;
			return;
		}

		// Check stop-loss exits (always, regardless of cooldown)
		if (Position > 0 && _stopPrice > 0 && candle.LowPrice <= _stopPrice)
		{
			SellMarket(Math.Abs(Position));
			_stopPrice = 0;
			_cooldownRemaining = CooldownBars;
		}
		else if (Position < 0 && _stopPrice > 0 && candle.HighPrice >= _stopPrice)
		{
			BuyMarket(Math.Abs(Position));
			_stopPrice = 0;
			_cooldownRemaining = CooldownBars;
		}

		var isFastAboveSlow = fastValue > slowValue;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			_wasFastAboveSlow = isFastAboveSlow;
			return;
		}

		// MA crossover entries
		if (_wasFastAboveSlow != isFastAboveSlow)
		{
			if (isFastAboveSlow && Position <= 0)
			{
				// Bullish cross - go long
				if (Position < 0)
					BuyMarket(Math.Abs(Position));
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice - atrValue * RiskM;
				_cooldownRemaining = CooldownBars;
			}
			else if (!isFastAboveSlow && Position >= 0)
			{
				// Bearish cross - go short
				if (Position > 0)
					SellMarket(Math.Abs(Position));
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = candle.ClosePrice + atrValue * RiskM;
				_cooldownRemaining = CooldownBars;
			}
		}

		_wasFastAboveSlow = isFastAboveSlow;
	}
}
