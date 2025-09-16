using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Absolutely No Lag LWMA strategy based on a double weighted moving average.
/// Opens long positions when the smoothed LWMA slope turns upward and closes shorts.
/// Opens short positions when the smoothed LWMA slope turns downward and closes longs.
/// </summary>
public class AbsolutelyNoLagLwmaStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<AppliedPriceType> _priceType;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<bool> _enableBuyEntries;
	private readonly StrategyParam<bool> _enableSellEntries;
	private readonly StrategyParam<bool> _enableBuyExits;
	private readonly StrategyParam<bool> _enableSellExits;
	private readonly StrategyParam<DataType> _candleType;

	private WeightedMovingAverage _primaryWma = null!;
	private WeightedMovingAverage _secondaryWma = null!;
	private readonly Queue<int> _colorHistory = new();
	private decimal _previousValue;
	private bool _hasPreviousValue;

	/// <summary>
	/// Length of both weighted moving averages.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Price source used for LWMA calculations.
	/// </summary>
	public AppliedPriceType PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <summary>
	/// Number of finished candles back used to generate signals.
	/// </summary>
	public int SignalBar
	{
		get => _signalBar.Value;
		set => _signalBar.Value = value;
	}

	/// <summary>
	/// Enables long entries when a bullish signal appears.
	/// </summary>
	public bool EnableBuyEntries
	{
		get => _enableBuyEntries.Value;
		set => _enableBuyEntries.Value = value;
	}

	/// <summary>
	/// Enables short entries when a bearish signal appears.
	/// </summary>
	public bool EnableSellEntries
	{
		get => _enableSellEntries.Value;
		set => _enableSellEntries.Value = value;
	}

	/// <summary>
	/// Enables closing long positions on bearish signals.
	/// </summary>
	public bool EnableBuyExits
	{
		get => _enableBuyExits.Value;
		set => _enableBuyExits.Value = value;
	}

	/// <summary>
	/// Enables closing short positions on bullish signals.
	/// </summary>
	public bool EnableSellExits
	{
		get => _enableSellExits.Value;
		set => _enableSellExits.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AbsolutelyNoLagLwmaStrategy"/> class.
	/// </summary>
	public AbsolutelyNoLagLwmaStrategy()
	{
		_length = Param(nameof(Length), 7)
		.SetGreaterThanZero()
		.SetDisplay("LWMA Length", "Period of the double LWMA", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(3, 20, 1);

		_priceType = Param(nameof(PriceType), AppliedPriceType.Close)
		.SetDisplay("Price Type", "Price source for LWMA", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetGreaterThanZero()
		.SetDisplay("Signal Bar", "Number of finished candles back for signals", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(1, 3, 1);

		_enableBuyEntries = Param(nameof(EnableBuyEntries), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableSellEntries = Param(nameof(EnableSellEntries), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_enableBuyExits = Param(nameof(EnableBuyExits), true)
		.SetDisplay("Enable Long Exits", "Allow closing long positions", "Trading");

		_enableSellExits = Param(nameof(EnableSellExits), true)
		.SetDisplay("Enable Short Exits", "Allow closing short positions", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for analysis", "General");
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

	_colorHistory.Clear();
	_hasPreviousValue = false;
	_previousValue = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_primaryWma = new WeightedMovingAverage { Length = Length };
	_secondaryWma = new WeightedMovingAverage { Length = Length };

	var subscription = SubscribeCandles(CandleType);

	subscription
	.Bind(ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _secondaryWma);
	DrawOwnTrades(area);
	}

	StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
	if (candle.State != CandleStates.Finished)
	return;

	var price = GetPrice(candle);

	var primaryValue = _primaryWma.Process(price, candle.OpenTime, true);
	var secondaryValue = _secondaryWma.Process(primaryValue.ToDecimal(), candle.OpenTime, true);

	if (!_secondaryWma.IsFormed)
	return;

	var currentValue = secondaryValue.ToDecimal();
	var color = 1;

	if (_hasPreviousValue)
	{
	if (currentValue > _previousValue)
	color = 2;
	else if (currentValue < _previousValue)
	color = 0;
	}
	else
	{
	_hasPreviousValue = true;
	}

	_previousValue = currentValue;

	_colorHistory.Enqueue(color);

	var maxHistory = Math.Max(3, SignalBar + 2);
	while (_colorHistory.Count > maxHistory)
	_colorHistory.Dequeue();

	if (_colorHistory.Count < maxHistory)
	return;

	var colors = _colorHistory.ToArray();
	var targetIndex = colors.Length - SignalBar - 1;

	if (targetIndex <= 0)
	return;

	var currentColor = colors[targetIndex];
	var previousColor = colors[targetIndex - 1];

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (EnableSellExits && currentColor == 2 && Position < 0)
	BuyMarket(Math.Abs(Position));

	if (EnableBuyExits && currentColor == 0 && Position > 0)
	SellMarket(Math.Abs(Position));

	if (EnableBuyEntries && currentColor == 2 && previousColor != 2 && Position <= 0)
	BuyMarket(Volume + Math.Abs(Position));

	if (EnableSellEntries && currentColor == 0 && previousColor != 0 && Position >= 0)
	SellMarket(Volume + Math.Abs(Position));
	}

	private decimal GetPrice(ICandleMessage candle)
	{
	var open = candle.OpenPrice;
	var high = candle.HighPrice;
	var low = candle.LowPrice;
	var close = candle.ClosePrice;

	return PriceType switch
	{
	AppliedPriceType.Close => close,
	AppliedPriceType.Open => open,
	AppliedPriceType.High => high,
	AppliedPriceType.Low => low,
	AppliedPriceType.Median => (high + low) / 2m,
	AppliedPriceType.Typical => (close + high + low) / 3m,
	AppliedPriceType.Weighted => (2m * close + high + low) / 4m,
	AppliedPriceType.Simpl => (open + close) / 2m,
	AppliedPriceType.Quarter => (open + close + high + low) / 4m,
	AppliedPriceType.TrendFollow0 => close > open ? high : close < open ? low : close,
	AppliedPriceType.TrendFollow1 => close > open ? (high + close) / 2m : close < open ? (low + close) / 2m : close,
	AppliedPriceType.Demark => CalculateDemarkPrice(open, high, low, close),
	_ => close
	};
	}

	private static decimal CalculateDemarkPrice(decimal open, decimal high, decimal low, decimal close)
	{
	var res = high + low + close;

	if (close < open)
	res = (res + low) / 2m;
	else if (close > open)
	res = (res + high) / 2m;
	else
	res = (res + close) / 2m;

	return ((res - low) + (res - high)) / 2m;
	}
}

/// <summary>
/// Available price sources matching the original MQL inputs.
/// </summary>
public enum AppliedPriceType
{
	Close = 1,
	Open,
	High,
	Low,
	Median,
	Typical,
	Weighted,
	Simpl,
	Quarter,
	TrendFollow0,
	TrendFollow1,
	Demark
}
