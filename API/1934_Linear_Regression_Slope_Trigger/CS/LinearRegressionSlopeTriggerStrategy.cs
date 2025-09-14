namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on linear regression slope and trigger line.
/// A long position opens when the trigger crosses above the slope.
/// A short position opens when the trigger crosses below the slope.
/// Positions close when an opposite relation appears.
/// </summary>
public class LinearRegressionSlopeTriggerStrategy : Strategy
{
	private readonly StrategyParam<int> _slopeLength;
	private readonly StrategyParam<int> _triggerShift;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private LinearRegression _slopeIndicator;
	private readonly List<decimal> _slopeHistory = new();
	private decimal _previousSlope;
	private decimal _previousTrigger;

	/// <summary>
	/// Period for calculating linear regression slope.
	/// </summary>
	public int SlopeLength
	{
		get => _slopeLength.Value;
		set => _slopeLength.Value = value;
	}

	/// <summary>
	/// Number of bars used for trigger calculation.
	/// </summary>
	public int TriggerShift
	{
		get => _triggerShift.Value;
		set => _triggerShift.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Take-profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="LinearRegressionSlopeTriggerStrategy"/>.
	/// </summary>
	public LinearRegressionSlopeTriggerStrategy()
	{
		_slopeLength = Param(nameof(SlopeLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Slope Length", "Period for linear regression slope", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_triggerShift = Param(nameof(TriggerShift), 1)
			.SetGreaterThanZero()
			.SetDisplay("Trigger Shift", "Bars to shift for trigger line", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit 	private readonly StrategyParam<int> _slopeLength;
	private readonly StrategyParam<int> _triggerShift;
	private readonly StrategyParam<bool> _enableLong;
	private readonly StrategyParam<bool> _enableShort;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private LinearRegression _slopeIndicator;
	private readonly List<decimal> _slopeHistory = new();
	private decimal _previousSlope;
	private decimal _previousTrigger;

	/// <summary>
	/// Period for calculating linear regression slope.
	/// </summary>
	public int SlopeLength
	{
		get => _slopeLength.Value;
		set => _slopeLength.Value = value;
	}

	/// <summary>
	/// Number of bars used for trigger calculation.
	/// </summary>
	public int TriggerShift
	{
		get => _triggerShift.Value;
		set => _triggerShift.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool EnableLong
	{
		get => _enableLong.Value;
		set => _enableLong.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool EnableShort
	{
		get => _enableShort.Value;
		set => _enableShort.Value = value;
	}

	/// <summary>
	/// Take-profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Type of candles used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="LinearRegressionSlopeTriggerStrategy"/>.
	/// </summary>
	public LinearRegressionSlopeTriggerStrategy()
	{
		_slopeLength = Param(nameof(SlopeLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Slope Length", "Period for linear regression slope", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_triggerShift = Param(nameof(TriggerShift), 1)
			.SetGreaterThanZero()
			.SetDisplay("Trigger Shift", "Bars to shift for trigger line", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_enableLong = Param(nameof(EnableLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_enableShort = Param(nameof(EnableShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit AlgoTrading//// <summary>
/// Take-profit percentage from entry price.
/// </summary>
public decimal TakeProfitPercent
{
get => _takeProfitPercent.Value;
set => _takeProfitPercent.Value = value;
}

/// <summary>
/// Stop-loss percentage from entry price.
/// </summary>
public decimal StopLossPercent
{
get => _stopLossPercent.Value;
set => _stopLossPercent.Value = value;
}

/// <summary>
/// Type of candles used by the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="LinearRegressionSlopeTriggerStrategy"/>.
/// </summary>
public LinearRegressionSlopeTriggerStrategy()
{
_slopeLength = Param(nameof(SlopeLength), 12)
.SetGreaterThanZero()
.SetDisplay("Slope Length", "Period for linear regression slope", "Indicator")
.SetCanOptimize(true)
.SetOptimize(5, 30, 1);

_triggerShift = Param(nameof(TriggerShift), 1)
.SetGreaterThanZero()
.SetDisplay("Trigger Shift", "Bars to shift for trigger line", "Indicator")
.SetCanOptimize(true)
.SetOptimize(1, 5, 1);

_enableLong = Param(nameof(EnableLong), true)
.SetDisplay("Enable Long", "Allow long trades", "Trading");

_enableShort = Param(nameof(EnableShort), true)
.SetDisplay("Enable Short", "Allow short trades", "Trading");

_takeProfitPercent = Param(nameof(TakeProfitPercent), 4m)
.SetGreaterThanZero()
.SetDisplay("Take Profit %", "Take-profit percentage", "Risk Management")
.SetCanOptimize(true)
.SetOptimize(2m, 10m, 1m);

_stopLossPercent = Param(nameof(StopLossPercent), 2m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
.SetCanOptimize(true)
.SetOptimize(1m, 5m, 1m);

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
.SetDisplay("Candle Type", "Timeframe for candles", "General");
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
_slopeHistory.Clear();
_previousSlope = 0m;
_previousTrigger = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_slopeIndicator = new LinearRegression { Length = SlopeLength };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

StartProtection(new Unit(TakeProfitPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent));

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _slopeIndicator);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
// Only finished candles are processed
if (candle.State != CandleStates.Finished)
return;

var typed = (LinearRegressionValue)_slopeIndicator.Process(candle.ClosePrice, candle.ServerTime, true);
if (!typed.IsFinal || typed.LinearReg is not decimal slope)
return;

// Store slope history for trigger calculation
_slopeHistory.Add(slope);

if (_slopeHistory.Count <= TriggerShift)
{
_previousSlope = slope;
_previousTrigger = slope;
return;
}

var delayedSlope = _slopeHistory[_slopeHistory.Count - 1 - TriggerShift];
var trigger = 2m * slope - delayedSlope;

if (_slopeHistory.Count > TriggerShift + 1)
_slopeHistory.RemoveAt(0);

// Ensure strategy can trade
if (!IsFormedAndOnlineAndAllowTrading())
{
_previousSlope = slope;
_previousTrigger = trigger;
return;
}

var buySignal = _previousTrigger <= _previousSlope && trigger > slope;
var sellSignal = _previousTrigger >= _previousSlope && trigger < slope;
var closeLong = slope > trigger;
var closeShort = trigger > slope;

if (closeLong && Position > 0)
SellMarket(Position);

if (closeShort && Position < 0)
BuyMarket(-Position);

if (buySignal && Position <= 0 && EnableLong)
BuyMarket(Volume + Math.Abs(Position));

if (sellSignal && Position >= 0 && EnableShort)
SellMarket(Volume + Math.Abs(Position));

_previousSlope = slope;
_previousTrigger = trigger;
}
}
