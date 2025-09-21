namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Mimics the "OsMA Four Colors Arrow" expert advisor using the MACD histogram zero-crossing as entry signal.
/// Opens trades on bullish or bearish momentum flips and applies optional trailing/stop-loss protection.
/// </summary>
public class OsMaFourColorsArrowStrategy : Strategy
{
/// <summary>
/// Trade direction restriction that mirrors the original expert inputs.
/// </summary>
public enum TradeDirectionMode
{
/// <summary>Allow only long positions.</summary>
LongOnly,

/// <summary>Allow only short positions.</summary>
ShortOnly,

/// <summary>Allow both long and short positions.</summary>
Both
}

private const decimal ExposureTolerance = 1e-8m;

private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<int> _signalPeriod;
private readonly StrategyParam<int> _stopLossPips;
private readonly StrategyParam<int> _takeProfitPips;
private readonly StrategyParam<int> _trailingActivatePips;
private readonly StrategyParam<int> _trailingStopPips;
private readonly StrategyParam<int> _trailingStepPips;
private readonly StrategyParam<int> _maxPositions;
private readonly StrategyParam<bool> _reverseSignals;
private readonly StrategyParam<TradeDirectionMode> _tradeMode;
private readonly StrategyParam<bool> _closeOpposite;
private readonly StrategyParam<bool> _onlyOnePosition;
private readonly StrategyParam<bool> _useTimeControl;
private readonly StrategyParam<int> _startHour;
private readonly StrategyParam<int> _startMinute;
private readonly StrategyParam<int> _endHour;
private readonly StrategyParam<int> _endMinute;
private readonly StrategyParam<decimal> _tradeVolume;

private MovingAverageConvergenceDivergenceHistogram? _macd;
private decimal? _previousHistogram;
private decimal _pipSize;

/// <summary>
/// Initializes a new instance of the strategy with defaults derived from the MQL version.
/// </summary>
public OsMaFourColorsArrowStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Timeframe used for signal generation", "General");

_fastPeriod = Param(nameof(FastPeriod), 12)
.SetDisplay("Fast EMA", "Fast EMA length for the MACD histogram", "Indicators")
.SetGreaterThanZero();

_slowPeriod = Param(nameof(SlowPeriod), 26)
.SetDisplay("Slow EMA", "Slow EMA length for the MACD histogram", "Indicators")
.SetGreaterThanZero();

_signalPeriod = Param(nameof(SignalPeriod), 9)
.SetDisplay("Signal EMA", "Signal smoothing length for the MACD histogram", "Indicators")
.SetGreaterThanZero();

_stopLossPips = Param(nameof(StopLossPips), 150)
.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips", "Risk Management");

_takeProfitPips = Param(nameof(TakeProfitPips), 460)
.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips", "Risk Management");

_trailingActivatePips = Param(nameof(TrailingActivatePips), 70)
.SetDisplay("Trailing Activate", "Profit in pips required before trailing starts", "Risk Management")
.SetCanOptimize(true);

_trailingStopPips = Param(nameof(TrailingStopPips), 250)
.SetDisplay("Trailing Stop (pips)", "Trailing stop distance expressed in pips", "Risk Management")
.SetCanOptimize(true);

_trailingStepPips = Param(nameof(TrailingStepPips), 50)
.SetDisplay("Trailing Step (pips)", "Minimal profit increase before the stop is tightened", "Risk Management")
.SetCanOptimize(true);

_maxPositions = Param(nameof(MaxPositions), 5)
.SetDisplay("Max Positions", "Maximum number of aggregated position units", "Trading")
.SetRange(0, 100);

_reverseSignals = Param(nameof(ReverseSignals), false)
.SetDisplay("Reverse Signals", "Invert buy and sell logic", "Trading");

_tradeMode = Param(nameof(DirectionMode), TradeDirectionMode.Both)
.SetDisplay("Trade Mode", "Allow longs, shorts, or both", "Trading");

_closeOpposite = Param(nameof(CloseOppositePositions), false)
.SetDisplay("Close Opposite", "Close opposite exposure before opening a new trade", "Trading");

_onlyOnePosition = Param(nameof(OnlyOnePosition), true)
.SetDisplay("Only One", "Allow only a single open direction at a time", "Trading");

_useTimeControl = Param(nameof(UseTimeControl), false)
.SetDisplay("Use Time Control", "Restrict trading to a daily session", "Trading Schedule");

_startHour = Param(nameof(StartHour), 10)
.SetDisplay("Start Hour", "Session start hour (0-23)", "Trading Schedule")
.SetRange(0, 23);

_startMinute = Param(nameof(StartMinute), 1)
.SetDisplay("Start Minute", "Session start minute", "Trading Schedule")
.SetRange(0, 59);

_endHour = Param(nameof(EndHour), 15)
.SetDisplay("End Hour", "Session end hour (0-23)", "Trading Schedule")
.SetRange(0, 23);

_endMinute = Param(nameof(EndMinute), 2)
.SetDisplay("End Minute", "Session end minute", "Trading Schedule")
.SetRange(0, 59);

_tradeVolume = Param(nameof(TradeVolume), 1m)
.SetDisplay("Trade Volume", "Order volume expressed in lots", "Trading")
.SetGreaterThanZero();

Volume = TradeVolume;
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
/// Fast EMA period for the MACD histogram.
/// </summary>
public int FastPeriod
{
get => _fastPeriod.Value;
set => _fastPeriod.Value = value;
}

/// <summary>
/// Slow EMA period for the MACD histogram.
/// </summary>
public int SlowPeriod
{
get => _slowPeriod.Value;
set => _slowPeriod.Value = value;
}

/// <summary>
/// Signal EMA smoothing period.
/// </summary>
public int SignalPeriod
{
get => _signalPeriod.Value;
set => _signalPeriod.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in pips.
/// </summary>
public int StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

/// <summary>
/// Take-profit distance expressed in pips.
/// </summary>
public int TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

/// <summary>
/// Profit threshold in pips that activates the trailing stop.
/// </summary>
public int TrailingActivatePips
{
get => _trailingActivatePips.Value;
set => _trailingActivatePips.Value = value;
}

/// <summary>
/// Trailing stop distance in pips.
/// </summary>
public int TrailingStopPips
{
get => _trailingStopPips.Value;
set => _trailingStopPips.Value = value;
}

/// <summary>
/// Additional profit required before the trailing stop is adjusted.
/// </summary>
public int TrailingStepPips
{
get => _trailingStepPips.Value;
set => _trailingStepPips.Value = value;
}

/// <summary>
/// Maximum number of position units (in trade volume multiples).
/// </summary>
public int MaxPositions
{
get => _maxPositions.Value;
set => _maxPositions.Value = value;
}

/// <summary>
/// Whether to invert the signal logic.
/// </summary>
public bool ReverseSignals
{
get => _reverseSignals.Value;
set => _reverseSignals.Value = value;
}

/// <summary>
/// Allowed trade direction.
/// </summary>
public TradeDirectionMode DirectionMode
{
get => _tradeMode.Value;
set => _tradeMode.Value = value;
}

/// <summary>
/// Whether to close opposite positions before opening a new one.
/// </summary>
public bool CloseOppositePositions
{
get => _closeOpposite.Value;
set => _closeOpposite.Value = value;
}

/// <summary>
/// Restrict the strategy to a single position at a time.
/// </summary>
public bool OnlyOnePosition
{
get => _onlyOnePosition.Value;
set => _onlyOnePosition.Value = value;
}

/// <summary>
/// Enable the intraday time filter.
/// </summary>
public bool UseTimeControl
{
get => _useTimeControl.Value;
set => _useTimeControl.Value = value;
}

/// <summary>
/// Session start hour.
/// </summary>
public int StartHour
{
get => _startHour.Value;
set => _startHour.Value = value;
}

/// <summary>
/// Session start minute.
/// </summary>
public int StartMinute
{
get => _startMinute.Value;
set => _startMinute.Value = value;
}

/// <summary>
/// Session end hour.
/// </summary>
public int EndHour
{
get => _endHour.Value;
set => _endHour.Value = value;
}

/// <summary>
/// Session end minute.
/// </summary>
public int EndMinute
{
get => _endMinute.Value;
set => _endMinute.Value = value;
}

/// <summary>
/// Order volume expressed in lots.
/// </summary>
public decimal TradeVolume
{
get => _tradeVolume.Value;
set
{
_tradeVolume.Value = value;
Volume = value;
}
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

_previousHistogram = null;
_macd = null;
_pipSize = 0m;
Volume = TradeVolume;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

ValidateTrailingParameters();

Volume = TradeVolume;
_pipSize = CalculatePipSize();

_macd = new MovingAverageConvergenceDivergenceHistogram
{
Macd =
{
ShortMa = { Length = FastPeriod },
LongMa = { Length = SlowPeriod }
},
SignalMa = { Length = SignalPeriod }
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_macd, ProcessCandle)
.Start();

var chartArea = CreateChartArea();
if (chartArea != null)
{
DrawCandles(chartArea, subscription);
if (_macd != null)
DrawIndicator(chartArea, _macd);
DrawOwnTrades(chartArea);
}

var stopLoss = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute) : null;
var takeProfit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute) : null;
var trailingStop = TrailingStopPips > 0 ? new Unit(TrailingStopPips * _pipSize, UnitTypes.Absolute) : null;
var trailingActivation = TrailingActivatePips > 0 ? new Unit(TrailingActivatePips * _pipSize, UnitTypes.Absolute) : null;
var trailingStep = TrailingStepPips > 0 ? new Unit(TrailingStepPips * _pipSize, UnitTypes.Absolute) : null;

StartProtection(
takeProfit: takeProfit,
stopLoss: stopLoss,
isStopTrailing: trailingStop != null,
trailingStop: trailingStop,
trailingStopActivation: trailingActivation,
trailingStopStep: trailingStep);
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue indicatorValue)
{
if (candle.State != CandleStates.Finished)
return;

if (_macd is null)
return;

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (!IsWithinSession(candle.OpenTime))
return;

if (indicatorValue is not MovingAverageConvergenceDivergenceHistogramValue macdValue)
return;

if (macdValue.Macd is not decimal histogram)
return;

var previous = _previousHistogram;
_previousHistogram = histogram;

if (previous is null)
return;

var crossUp = previous <= 0m && histogram > 0m;
var crossDown = previous >= 0m && histogram < 0m;

if (!crossUp && !crossDown)
return;

var longSignal = crossUp;
var shortSignal = crossDown;

if (ReverseSignals)
{
(longSignal, shortSignal) = (shortSignal, longSignal);
}

if (longSignal)
{
TryEnterLong();
}
else if (shortSignal)
{
TryEnterShort();
}
}

private void TryEnterLong()
{
if (DirectionMode == TradeDirectionMode.ShortOnly)
return;

if (TradeVolume <= 0m)
return;

if (Position < 0m)
{
if (CloseOppositePositions)
{
var volumeToCover = Math.Abs(Position);
if (volumeToCover > 0m)
BuyMarket(volumeToCover);
}

return;
}

if (OnlyOnePosition && Position > 0m)
return;

if (!HasCapacityForNewEntry())
return;

BuyMarket(TradeVolume);
}

private void TryEnterShort()
{
if (DirectionMode == TradeDirectionMode.LongOnly)
return;

if (TradeVolume <= 0m)
return;

if (Position > 0m)
{
if (CloseOppositePositions)
{
var volumeToCover = Math.Abs(Position);
if (volumeToCover > 0m)
SellMarket(volumeToCover);
}

return;
}

if (OnlyOnePosition && Position < 0m)
return;

if (!HasCapacityForNewEntry())
return;

SellMarket(TradeVolume);
}

private bool HasCapacityForNewEntry()
{
if (MaxPositions <= 0)
return true;

var maxExposure = MaxPositions * TradeVolume;
if (maxExposure <= 0m)
return true;

var currentExposure = Math.Abs(Position);
return currentExposure + TradeVolume <= maxExposure + ExposureTolerance;
}

private bool IsWithinSession(DateTimeOffset time)
{
if (!UseTimeControl)
return true;

var start = new TimeSpan(StartHour, StartMinute, 0);
var end = new TimeSpan(EndHour, EndMinute, 0);
var current = time.TimeOfDay;

if (start == end)
return false;

return start < end
? current >= start && current < end
: current >= start || current < end;
}

private decimal CalculatePipSize()
{
var step = Security?.PriceStep ?? 0m;
if (step <= 0m)
step = 1m;

var decimals = Security?.Decimals ?? 0;
return decimals is 3 or 5 ? step * 10m : step;
}

private void ValidateTrailingParameters()
{
if (TrailingStopPips > 0 && TrailingStepPips <= 0)
throw new InvalidOperationException("Trailing step must be positive when trailing stop is enabled.");

if (TrailingStopPips > 0 && TrailingActivatePips <= 0)
throw new InvalidOperationException("Trailing activation must be positive when trailing stop is enabled.");
}
}
