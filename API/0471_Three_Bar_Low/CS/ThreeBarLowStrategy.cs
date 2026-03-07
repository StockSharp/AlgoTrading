namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// 3-Bar Low Strategy.
/// Buys when price breaks below recent 3-bar low (mean reversion).
/// Exits when price breaks above recent 7-bar high.
/// Uses EMA as optional trend filter.
/// </summary>
public class ThreeBarLowStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _lookbackLow;
	private readonly StrategyParam<int> _lookbackHigh;
	private readonly StrategyParam<int> _cooldownBars;

	private ExponentialMovingAverage _ema;

	private readonly List<decimal> _lows = new();
	private readonly List<decimal> _highs = new();
	private int _cooldownRemaining;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int LookbackLow
	{
		get => _lookbackLow.Value;
		set => _lookbackLow.Value = value;
	}

	public int LookbackHigh
	{
		get => _lookbackHigh.Value;
		set => _lookbackHigh.Value = value;
	}

	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	public ThreeBarLowStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

		_lookbackLow = Param(nameof(LookbackLow), 3)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Low", "Bars for lowest low", "Parameters");

		_lookbackHigh = Param(nameof(LookbackHigh), 7)
			.SetGreaterThanZero()
			.SetDisplay("Lookback High", "Bars for highest high", "Parameters");

		_cooldownBars = Param(nameof(CooldownBars), 10)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_lows.Clear();
		_highs.Clear();
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal emaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
			return;

		// Track lows and highs
		_lows.Add(candle.LowPrice);
		_highs.Add(candle.HighPrice);

		if (_lows.Count > LookbackLow + 1)
			_lows.RemoveAt(0);
		if (_highs.Count > LookbackHigh + 1)
			_highs.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldownRemaining > 0)
		{
			_cooldownRemaining--;
			return;
		}

		if (_lows.Count <= LookbackLow || _highs.Count <= LookbackHigh)
			return;

		// Find lowest low of previous N bars (excluding current)
		var lowestLow = decimal.MaxValue;
		for (var i = 0; i < _lows.Count - 1; i++)
			lowestLow = Math.Min(lowestLow, _lows[i]);

		// Find highest high of previous N bars (excluding current)
		var highestHigh = decimal.MinValue;
		for (var i = 0; i < _highs.Count - 1; i++)
			highestHigh = Math.Max(highestHigh, _highs[i]);

		var price = candle.ClosePrice;

		// Buy: price breaks below previous N-bar low (mean reversion)
		if (price < lowestLow && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
			BuyMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Sell short: price breaks above previous N-bar high
		else if (price > highestHigh && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Math.Abs(Position));
			SellMarket(Volume);
			_cooldownRemaining = CooldownBars;
		}
		// Exit long: price above previous high
		else if (Position > 0 && price > highestHigh)
		{
			SellMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
		// Exit short: price below previous low
		else if (Position < 0 && price < lowestLow)
		{
			BuyMarket(Math.Abs(Position));
			_cooldownRemaining = CooldownBars;
		}
	}
}
