namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Three Red / Three Green Strategy with volatility filter.
/// Buys after 3 red candles (mean reversion) with ATR > average.
/// Exits after 3 green candles or max hold period.
/// </summary>
public class ThreeRedGreenVolatilityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maxTradeDuration;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _cooldownBars;

	private AverageTrueRange _atr;
	private SimpleMovingAverage _atrAvg;

	private int _redCount;
	private int _greenCount;
	private int _barsSinceEntry;
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MaxTradeDuration
	{
		get => _maxTradeDuration.Value;
		set => _maxTradeDuration.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ThreeRedGreenVolatilityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maxTradeDuration = Param(nameof(MaxTradeDuration), 20)
			.SetGreaterThanZero()
			.SetDisplay("Max Hold Bars", "Maximum bars in position", "Trading");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 12)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_atr = null;
		_atrAvg = null;
		_redCount = 0;
		_greenCount = 0;
		_barsSinceEntry = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_atr = new AverageTrueRange { Length = AtrPeriod };
		_atrAvg = new SimpleMovingAverage { Length = 30 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_atr, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal atrVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_atr.IsFormed)
			return;

		// Update ATR average manually
		var atrAvgResult = _atrAvg.Process(new DecimalIndicatorValue(_atrAvg, atrVal, candle.ServerTime));
		var atrAvgVal = _atrAvg.IsFormed ? atrAvgResult.ToDecimal() : atrVal;

		var isRed = candle.ClosePrice < candle.OpenPrice;
		var isGreen = candle.ClosePrice > candle.OpenPrice;

		_redCount = isRed ? _redCount + 1 : 0;
		_greenCount = isGreen ? _greenCount + 1 : 0;

		if (Position != 0)
			_barsSinceEntry++;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		var highVol = atrVal > atrAvgVal * 0.8m;

		// Buy after 3 red candles + volatility check
		if (_redCount >= 3 && highVol && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_barsSinceEntry = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Sell after 3 green candles + high volatility
		else if (_greenCount >= 3 && highVol && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_barsSinceEntry = 0;
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: 3 green candles or max hold
		else if (Position > 0 && (_greenCount >= 3 || _barsSinceEntry >= MaxTradeDuration))
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: 3 red candles or max hold
		else if (Position < 0 && (_redCount >= 3 || _barsSinceEntry >= MaxTradeDuration))
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
