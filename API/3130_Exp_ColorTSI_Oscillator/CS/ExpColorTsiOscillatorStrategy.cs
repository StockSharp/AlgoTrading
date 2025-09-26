namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Reflection;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MetaTrader 5 expert advisor Exp_ColorTSI-Oscillator converted to StockSharp.
/// Rebuilds the double-smoothed True Strength Index oscillator with configurable smoothing algorithms
/// and acts when the oscillator turns up or down relative to its delayed trigger line.
/// </summary>
public class ExpColorTsiOscillatorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<ColorTsiSmoothingMethod> _firstMethod;
	private readonly StrategyParam<int> _firstLength;
	private readonly StrategyParam<int> _firstPhase;
	private readonly StrategyParam<ColorTsiSmoothingMethod> _secondMethod;
	private readonly StrategyParam<int> _secondLength;
	private readonly StrategyParam<int> _secondPhase;
	private readonly StrategyParam<ColorTsiAppliedPrice> _priceMode;
	private readonly StrategyParam<int> _signalBar;
	private readonly StrategyParam<int> _triggerShift;
	private readonly StrategyParam<bool> _enableLongEntries;
	private readonly StrategyParam<bool> _enableShortEntries;
	private readonly StrategyParam<bool> _enableLongExits;
	private readonly StrategyParam<bool> _enableShortExits;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private IIndicator _firstMomentumSmoother = null!;
	private IIndicator _firstAbsoluteSmoother = null!;
	private IIndicator _secondMomentumSmoother = null!;
	private IIndicator _secondAbsoluteSmoother = null!;

	private readonly List<decimal> _tsiHistory = new();
	private readonly List<DateTimeOffset> _tsiTimes = new();

	private decimal? _previousPrice;
	private TimeSpan _timeFrame;
	private DateTimeOffset? _lastLongEntryTime;
	private DateTimeOffset? _lastShortEntryTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="ExpColorTsiOscillatorStrategy"/> class.
	/// </summary>
	public ExpColorTsiOscillatorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe used for signal calculations", "General");


		_firstMethod = Param(nameof(FirstMethod), ColorTsiSmoothingMethod.Sma)
		.SetDisplay("Momentum Smoother", "Smoothing method applied to price momentum", "Indicator");

		_firstLength = Param(nameof(FirstLength), 12)
		.SetDisplay("Momentum Length", "Length of the first smoothing stage", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(2, 60, 1);

		_firstPhase = Param(nameof(FirstPhase), 15)
		.SetDisplay("Momentum Phase", "Phase parameter for Jurik-style averages", "Indicator")
		.SetRange(-100, 100);

		_secondMethod = Param(nameof(SecondMethod), ColorTsiSmoothingMethod.Sma)
		.SetDisplay("Signal Smoother", "Smoothing method applied to the second stage", "Indicator");

		_secondLength = Param(nameof(SecondLength), 12)
		.SetDisplay("Signal Length", "Length of the second smoothing stage", "Indicator")
		.SetGreaterThanZero()
		.SetCanOptimize(true)
		.SetOptimize(2, 60, 1);

		_secondPhase = Param(nameof(SecondPhase), 15)
		.SetDisplay("Signal Phase", "Phase parameter for the second stage", "Indicator")
		.SetRange(-100, 100);

		_priceMode = Param(nameof(PriceMode), ColorTsiAppliedPrice.Close)
		.SetDisplay("Applied Price", "Price source fed to the oscillator", "Indicator");

		_signalBar = Param(nameof(SignalBar), 1)
		.SetDisplay("Signal Bar", "Offset of the bar used for decision making", "Signals")
		.SetNotNegative();

		_triggerShift = Param(nameof(TriggerShift), 1)
		.SetDisplay("Trigger Shift", "Delay applied to the trigger line", "Signals")
		.SetNotNegative();

		_enableLongEntries = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable Long Entries", "Allow opening long positions", "Trading");

		_enableShortEntries = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable Short Entries", "Allow opening short positions", "Trading");

		_enableLongExits = Param(nameof(EnableLongExits), true)
		.SetDisplay("Enable Long Exits", "Allow closing long positions on opposite signal", "Trading");

		_enableShortExits = Param(nameof(EnableShortExits), true)
		.SetDisplay("Enable Short Exits", "Allow closing short positions on opposite signal", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 1000m)
		.SetDisplay("Stop Loss (points)", "Protective stop distance expressed in price points", "Risk")
		.SetNotNegative();

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
		.SetDisplay("Take Profit (points)", "Profit target distance expressed in price points", "Risk")
		.SetNotNegative();
	}

/// <summary>
/// Candle type processed by the strategy.
/// </summary>
public DataType CandleType
{
	get => _candleType.Value;
	set => _candleType.Value = value;
}


/// <summary>
/// Smoothing algorithm applied to raw momentum values.
/// </summary>
public ColorTsiSmoothingMethod FirstMethod
{
get => _firstMethod.Value;
set => _firstMethod.Value = value;
}

/// <summary>
/// Length of the momentum smoothing stage.
/// </summary>
public int FirstLength
{
get => _firstLength.Value;
set => _firstLength.Value = value;
}

/// <summary>
/// Phase parameter for the first smoothing stage.
/// </summary>
public int FirstPhase
{
get => _firstPhase.Value;
set => _firstPhase.Value = value;
}

/// <summary>
/// Smoothing algorithm applied to the second stage.
/// </summary>
public ColorTsiSmoothingMethod SecondMethod
{
get => _secondMethod.Value;
set => _secondMethod.Value = value;
}

/// <summary>
/// Length of the second smoothing stage.
/// </summary>
public int SecondLength
{
get => _secondLength.Value;
set => _secondLength.Value = value;
}

/// <summary>
/// Phase parameter for the second smoothing stage.
/// </summary>
public int SecondPhase
{
get => _secondPhase.Value;
set => _secondPhase.Value = value;
}

/// <summary>
/// Applied price used as oscillator input.
/// </summary>
public ColorTsiAppliedPrice PriceMode
{
get => _priceMode.Value;
set => _priceMode.Value = value;
}

/// <summary>
/// Bar index used for evaluating signals.
/// </summary>
public int SignalBar
{
get => _signalBar.Value;
set => _signalBar.Value = value;
}

/// <summary>
/// Number of bars used to delay the trigger line.
/// </summary>
public int TriggerShift
{
get => _triggerShift.Value;
set => _triggerShift.Value = value;
}

/// <summary>
/// Enables opening long positions.
/// </summary>
public bool EnableLongEntries
{
get => _enableLongEntries.Value;
set => _enableLongEntries.Value = value;
}

/// <summary>
/// Enables opening short positions.
/// </summary>
public bool EnableShortEntries
{
get => _enableShortEntries.Value;
set => _enableShortEntries.Value = value;
}

/// <summary>
/// Enables closing long positions when the oscillator turns down.
/// </summary>
public bool EnableLongExits
{
get => _enableLongExits.Value;
set => _enableLongExits.Value = value;
}

/// <summary>
/// Enables closing short positions when the oscillator turns up.
/// </summary>
public bool EnableShortExits
{
get => _enableShortExits.Value;
set => _enableShortExits.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in instrument points.
/// </summary>
public decimal StopLossPoints
{
get => _stopLossPoints.Value;
set => _stopLossPoints.Value = value;
}

/// <summary>
/// Take-profit distance expressed in instrument points.
/// </summary>
public decimal TakeProfitPoints
{
get => _takeProfitPoints.Value;
set => _takeProfitPoints.Value = value;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

ResetState();

_firstMomentumSmoother = CreateSmoother(FirstMethod, FirstLength, FirstPhase);
_firstAbsoluteSmoother = CreateSmoother(FirstMethod, FirstLength, FirstPhase);
_secondMomentumSmoother = CreateSmoother(SecondMethod, SecondLength, SecondPhase);
_secondAbsoluteSmoother = CreateSmoother(SecondMethod, SecondLength, SecondPhase);

_timeFrame = CandleType.Arg is TimeSpan frame ? frame : TimeSpan.Zero;

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();

var step = Security?.PriceStep ?? 1m;
if (step <= 0m)
step = 1m;

var takeProfit = TakeProfitPoints > 0m ? new Unit(TakeProfitPoints * step, UnitTypes.Point) : null;
var stopLoss = StopLossPoints > 0m ? new Unit(StopLossPoints * step, UnitTypes.Point) : null;

StartProtection(takeProfit, stopLoss);

var area = CreateChartArea();
if (area != null)
{
	DrawCandles(area, subscription);
	DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

var price = GetAppliedPrice(candle, PriceMode);

if (_previousPrice is null)
{
_previousPrice = price;
return;
}

var diff = price - _previousPrice.Value;
var absDiff = Math.Abs(diff);
var time = candle.OpenTime;

var firstMomentum = _firstMomentumSmoother.Process(time, diff);
if (!firstMomentum.IsFinal || firstMomentum.Value is not decimal firstMomentumValue)
{
_previousPrice = price;
return;
}

var firstAbsolute = _firstAbsoluteSmoother.Process(time, absDiff);
if (!firstAbsolute.IsFinal || firstAbsolute.Value is not decimal firstAbsoluteValue)
{
_previousPrice = price;
return;
}

var secondMomentum = _secondMomentumSmoother.Process(time, firstMomentumValue);
if (!secondMomentum.IsFinal || secondMomentum.Value is not decimal secondMomentumValue)
{
_previousPrice = price;
return;
}

var secondAbsolute = _secondAbsoluteSmoother.Process(time, firstAbsoluteValue);
if (!secondAbsolute.IsFinal || secondAbsolute.Value is not decimal secondAbsoluteValue || secondAbsoluteValue == 0m)
{
_previousPrice = price;
return;
}

var tsi = 100m * secondMomentumValue / secondAbsoluteValue;

_tsiHistory.Insert(0, tsi);
_tsiTimes.Insert(0, candle.OpenTime);

var maxHistory = Math.Max(3, Math.Max(SignalBar + TriggerShift + 3, SignalBar + 3));
if (_tsiHistory.Count > maxHistory)
{
_tsiHistory.RemoveRange(maxHistory, _tsiHistory.Count - maxHistory);
_tsiTimes.RemoveRange(maxHistory, _tsiTimes.Count - maxHistory);
}

_previousPrice = price;

if (!IsFormedAndOnlineAndAllowTrading())
return;

EvaluateSignals();
}

private void EvaluateSignals()
{
var signalBar = Math.Max(0, SignalBar);
var triggerShift = Math.Max(0, TriggerShift);
var previousIndex = signalBar + 1;
var triggerIndex = signalBar - triggerShift;
var previousTriggerIndex = signalBar + 1 - triggerShift;

if (triggerIndex < 0 || previousTriggerIndex < 0)
return;

if (_tsiHistory.Count <= Math.Max(previousIndex, Math.Max(triggerIndex, previousTriggerIndex)))
return;

var currentTsi = _tsiHistory[signalBar];
var previousTsi = _tsiHistory[previousIndex];
var currentTrigger = _tsiHistory[triggerIndex];
var previousTrigger = _tsiHistory[previousTriggerIndex];

var isBullish = previousTsi > previousTrigger;
var isBearish = previousTsi < previousTrigger;

var shouldCloseShort = EnableShortExits && isBullish;
var shouldCloseLong = EnableLongExits && isBearish;
var shouldOpenLong = EnableLongEntries && isBullish && currentTsi <= currentTrigger;
var shouldOpenShort = EnableShortEntries && isBearish && currentTsi >= currentTrigger;

if (shouldCloseLong && Position > 0)
{
SellMarket(Position);
}

if (shouldCloseShort && Position < 0)
{
BuyMarket(Math.Abs(Position));
}

if (Volume <= 0m)
return;

if (!shouldOpenLong && !shouldOpenShort)
return;

var executionTime = GetSignalExecutionTime(signalBar);

if (shouldOpenLong && Position <= 0 && (_lastLongEntryTime != executionTime))
{
var orderVolume = Volume + Math.Abs(Position);
if (orderVolume > 0m)
{
	BuyMarket(orderVolume);
	_lastLongEntryTime = executionTime;
}
}

if (shouldOpenShort && Position >= 0 && (_lastShortEntryTime != executionTime))
{
var orderVolume = Volume + Math.Abs(Position);
if (orderVolume > 0m)
{
SellMarket(orderVolume);
_lastShortEntryTime = executionTime;
}
}
}

private DateTimeOffset GetSignalExecutionTime(int signalBar)
{
if (signalBar >= _tsiTimes.Count)
return default;

var signalTime = _tsiTimes[signalBar];
return _timeFrame > TimeSpan.Zero ? signalTime + _timeFrame : signalTime;
}

private void ResetState()
{
_tsiHistory.Clear();
_tsiTimes.Clear();
_previousPrice = null;
_lastLongEntryTime = null;
_lastShortEntryTime = null;
}

private static decimal GetAppliedPrice(ICandleMessage candle, ColorTsiAppliedPrice priceMode)
{
return priceMode switch
{
ColorTsiAppliedPrice.Close => candle.ClosePrice,
ColorTsiAppliedPrice.Open => candle.OpenPrice,
ColorTsiAppliedPrice.High => candle.HighPrice,
ColorTsiAppliedPrice.Low => candle.LowPrice,
ColorTsiAppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
ColorTsiAppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
ColorTsiAppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
ColorTsiAppliedPrice.Simple => (candle.OpenPrice + candle.ClosePrice) / 2m,
ColorTsiAppliedPrice.Quarter => (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / 4m,
ColorTsiAppliedPrice.TrendFollow0 => candle.ClosePrice > candle.OpenPrice ? candle.HighPrice : candle.ClosePrice < candle.OpenPrice ? candle.LowPrice : candle.ClosePrice,
ColorTsiAppliedPrice.TrendFollow1 => candle.ClosePrice > candle.OpenPrice ? (candle.HighPrice + candle.ClosePrice) / 2m : candle.ClosePrice < candle.OpenPrice ? (candle.LowPrice + candle.ClosePrice) / 2m : candle.ClosePrice,
ColorTsiAppliedPrice.Demark => CalculateDemarkPrice(candle),
_ => candle.ClosePrice,
};
}

private static decimal CalculateDemarkPrice(ICandleMessage candle)
{
var sum = candle.HighPrice + candle.LowPrice + candle.ClosePrice;

if (candle.ClosePrice < candle.OpenPrice)
sum = (sum + candle.LowPrice) / 2m;
else if (candle.ClosePrice > candle.OpenPrice)
sum = (sum + candle.HighPrice) / 2m;
else
sum = (sum + candle.ClosePrice) / 2m;

return ((sum - candle.LowPrice) + (sum - candle.HighPrice)) / 2m;
}

private static IIndicator CreateSmoother(ColorTsiSmoothingMethod method, int length, int phase)
{
var normalizedLength = Math.Max(1, length);
var offset = 0.5m + phase / 200m;
offset = Math.Max(0m, Math.Min(1m, offset));

return method switch
{
ColorTsiSmoothingMethod.Sma => new SimpleMovingAverage { Length = normalizedLength },
ColorTsiSmoothingMethod.Ema => new ExponentialMovingAverage { Length = normalizedLength },
ColorTsiSmoothingMethod.Smma => new SmoothedMovingAverage { Length = normalizedLength },
ColorTsiSmoothingMethod.Lwma => new WeightedMovingAverage { Length = normalizedLength },
ColorTsiSmoothingMethod.Jjma => CreateJurik(normalizedLength, phase),
ColorTsiSmoothingMethod.Jurx => new ZeroLagExponentialMovingAverage { Length = normalizedLength },
ColorTsiSmoothingMethod.Parma => new ArnaudLegouxMovingAverage { Length = normalizedLength, Offset = offset, Sigma = 6m },
ColorTsiSmoothingMethod.T3 => new TripleExponentialMovingAverage { Length = normalizedLength },
ColorTsiSmoothingMethod.Vidya => new ExponentialMovingAverage { Length = normalizedLength },
ColorTsiSmoothingMethod.Ama => new KaufmanAdaptiveMovingAverage { Length = normalizedLength },
_ => new SimpleMovingAverage { Length = normalizedLength },
};
}

private static IIndicator CreateJurik(int length, int phase)
{
var jurik = new JurikMovingAverage { Length = Math.Max(1, length) };
var property = jurik.GetType().GetProperty("Phase", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
if (property != null && property.CanWrite)
{
var clamped = Math.Max(-100, Math.Min(100, phase));
property.SetValue(jurik, clamped);
}

return jurik;
}
}

/// <summary>
/// Supported smoothing methods for the ColorTSI oscillator.
/// </summary>
public enum ColorTsiSmoothingMethod
{
/// <summary>Simple moving average.</summary>
Sma,
/// <summary>Exponential moving average.</summary>
Ema,
/// <summary>Smoothed moving average.</summary>
Smma,
/// <summary>Linear weighted moving average.</summary>
Lwma,
/// <summary>Jurik moving average.</summary>
Jjma,
/// <summary>Zero-lag exponential moving average (JurX approximation).</summary>
Jurx,
/// <summary>Parabolic moving average (ALMA approximation).</summary>
Parma,
/// <summary>Tillson T3 moving average.</summary>
T3,
/// <summary>VIDYA moving average approximation.</summary>
Vidya,
/// <summary>Kaufman adaptive moving average.</summary>
Ama
}

/// <summary>
/// Applied price options mirroring the original indicator.
/// </summary>
public enum ColorTsiAppliedPrice
{
/// <summary>Close price.</summary>
Close,
/// <summary>Open price.</summary>
Open,
/// <summary>High price.</summary>
High,
/// <summary>Low price.</summary>
Low,
/// <summary>Median price (high + low) / 2.</summary>
Median,
/// <summary>Typical price (high + low + close) / 3.</summary>
Typical,
/// <summary>Weighted price (high + low + close * 2) / 4.</summary>
Weighted,
/// <summary>Simple average of open and close.</summary>
Simple,
/// <summary>Quarted price (open + high + low + close) / 4.</summary>
Quarter,
/// <summary>Trend-following price variant 0.</summary>
TrendFollow0,
/// <summary>Trend-following price variant 1.</summary>
TrendFollow1,
/// <summary>Demark price.</summary>
Demark
}
