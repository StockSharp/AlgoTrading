using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tipu MACD expert advisor converted from MQL4 to StockSharp.
/// Generates entries from MACD zero-line or signal-line crossovers
/// with optional time filtering, protective orders, trailing stop, and breakeven logic.
/// </summary>
public class TipuMacdEaStrategy : Strategy
{
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<decimal> _tradeVolume;
private readonly StrategyParam<decimal> _maxPositionVolume;
private readonly StrategyParam<bool> _useTimeFilter;
private readonly StrategyParam<int> _zone1StartHour;
private readonly StrategyParam<int> _zone1EndHour;
private readonly StrategyParam<int> _zone2StartHour;
private readonly StrategyParam<int> _zone2EndHour;
private readonly StrategyParam<int> _fastPeriod;
private readonly StrategyParam<int> _slowPeriod;
private readonly StrategyParam<int> _signalPeriod;
private readonly StrategyParam<int> _macdShift;
private readonly StrategyParam<bool> _useZeroCross;
private readonly StrategyParam<bool> _useSignalCross;
private readonly StrategyParam<bool> _allowHedging;
private readonly StrategyParam<bool> _closeOnReverseSignal;
private readonly StrategyParam<bool> _useTakeProfit;
private readonly StrategyParam<decimal> _takeProfitPips;
private readonly StrategyParam<bool> _useStopLoss;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<bool> _useTrailingStop;
private readonly StrategyParam<decimal> _trailingPips;
private readonly StrategyParam<decimal> _trailingCushionPips;
private readonly StrategyParam<bool> _useRiskFree;
private readonly StrategyParam<decimal> _riskFreePips;

private MovingAverageConvergenceDivergenceSignal _macd;
private decimal? _macdCurrent;
private decimal? _macdPrevious;
private decimal? _macdPrevPrev;
private decimal? _signalCurrent;
private decimal? _signalPrevious;
private decimal? _signalPrevPrev;

private decimal? _longEntryPrice;
private decimal? _shortEntryPrice;
private decimal? _longStopPrice;
private decimal? _shortStopPrice;
private decimal? _longTakeProfitPrice;
private decimal? _shortTakeProfitPrice;

private decimal _pipSize;

/// <summary>
/// Initializes a new instance of <see cref="TipuMacdEaStrategy"/>.
/// </summary>
public TipuMacdEaStrategy()
{
_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
.SetDisplay("Candle Type", "Primary candle series for MACD", "General");

_tradeVolume = Param(nameof(TradeVolume), 0.01m)
.SetGreaterThanZero()
.SetDisplay("Trade Volume", "Volume used for each market order", "Trading");

_maxPositionVolume = Param(nameof(MaxPositionVolume), 0.05m)
.SetGreaterThanZero()
.SetDisplay("Max Position Volume", "Maximum cumulative position volume", "Trading");

_useTimeFilter = Param(nameof(UseTimeFilter), true)
.SetDisplay("Use Time Filter", "Enable trading-hour windows", "Filters");

_zone1StartHour = Param(nameof(Zone1StartHour), 10)
.SetRange(0, 23)
.SetDisplay("Zone 1 Start", "First trading window start hour", "Filters");

_zone1EndHour = Param(nameof(Zone1EndHour), 18)
.SetRange(0, 23)
.SetDisplay("Zone 1 End", "First trading window end hour", "Filters");

_zone2StartHour = Param(nameof(Zone2StartHour), 1)
.SetRange(0, 23)
.SetDisplay("Zone 2 Start", "Second trading window start hour", "Filters");

_zone2EndHour = Param(nameof(Zone2EndHour), 6)
.SetRange(0, 23)
.SetDisplay("Zone 2 End", "Second trading window end hour", "Filters");

_fastPeriod = Param(nameof(FastPeriod), 12)
.SetGreaterThanZero()
.SetDisplay("Fast EMA", "Fast EMA length for MACD", "Indicators")
.SetCanOptimize(true);

_slowPeriod = Param(nameof(SlowPeriod), 26)
.SetGreaterThanZero()
.SetDisplay("Slow EMA", "Slow EMA length for MACD", "Indicators")
.SetCanOptimize(true);

_signalPeriod = Param(nameof(SignalPeriod), 9)
.SetGreaterThanZero()
.SetDisplay("Signal SMA", "Signal smoothing length", "Indicators")
.SetCanOptimize(true);

_macdShift = Param(nameof(MacdShift), 1)
.SetRange(0, 1)
.SetDisplay("MACD Shift", "Shift used when evaluating crossovers", "Indicators");

_useZeroCross = Param(nameof(UseZeroCross), false)
.SetDisplay("Use Zero Cross", "Enable zero-line crossover signals", "Signals");

_useSignalCross = Param(nameof(UseSignalCross), true)
.SetDisplay("Use Signal Cross", "Enable MACD/Signal crossover signals", "Signals");

_allowHedging = Param(nameof(AllowHedging), false)
.SetDisplay("Allow Hedging", "Allow scaling into both directions", "Trading");

_closeOnReverseSignal = Param(nameof(CloseOnReverseSignal), true)
.SetDisplay("Close On Reverse", "Close opposite position when a reversal signal appears", "Trading");

_useTakeProfit = Param(nameof(UseTakeProfit), true)
.SetDisplay("Use Take Profit", "Enable take-profit target", "Risk");

_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
.SetGreaterThanZero()
.SetDisplay("Take Profit (pips)", "Take-profit distance in pips", "Risk");

_useStopLoss = Param(nameof(UseStopLoss), false)
.SetDisplay("Use Stop Loss", "Enable protective stop-loss", "Risk");

_stopLossPips = Param(nameof(StopLossPips), 50m)
.SetGreaterThanZero()
.SetDisplay("Stop Loss (pips)", "Stop-loss distance in pips", "Risk");

_useTrailingStop = Param(nameof(UseTrailingStop), true)
.SetDisplay("Use Trailing", "Enable trailing stop management", "Risk");

_trailingPips = Param(nameof(TrailingPips), 10m)
.SetGreaterThanZero()
.SetDisplay("Trailing Distance (pips)", "Distance kept when trailing", "Risk");

_trailingCushionPips = Param(nameof(TrailingCushionPips), 5m)
.SetNotNegative()
.SetDisplay("Trailing Cushion (pips)", "Extra buffer required before trailing", "Risk");

_useRiskFree = Param(nameof(UseRiskFree), true)
.SetDisplay("Use Breakeven", "Move stop to entry after sufficient profit", "Risk");

_riskFreePips = Param(nameof(RiskFreePips), 10m)
.SetGreaterThanZero()
.SetDisplay("Breakeven Trigger (pips)", "Profit required to move stop to entry", "Risk");

_pipSize = 0m;
}

/// <summary>
/// Candle type used for calculations.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Volume of each market order.
/// </summary>
public decimal TradeVolume
{
get => _tradeVolume.Value;
set => _tradeVolume.Value = value;
}

/// <summary>
/// Maximum cumulative position size.
/// </summary>
public decimal MaxPositionVolume
{
get => _maxPositionVolume.Value;
set => _maxPositionVolume.Value = value;
}

/// <summary>
/// Indicates if trading hours filter is enabled.
/// </summary>
public bool UseTimeFilter
{
get => _useTimeFilter.Value;
set => _useTimeFilter.Value = value;
}

/// <summary>
/// First trading zone start hour.
/// </summary>
public int Zone1StartHour
{
get => _zone1StartHour.Value;
set => _zone1StartHour.Value = value;
}

/// <summary>
/// First trading zone end hour.
/// </summary>
public int Zone1EndHour
{
get => _zone1EndHour.Value;
set => _zone1EndHour.Value = value;
}

/// <summary>
/// Second trading zone start hour.
/// </summary>
public int Zone2StartHour
{
get => _zone2StartHour.Value;
set => _zone2StartHour.Value = value;
}

/// <summary>
/// Second trading zone end hour.
/// </summary>
public int Zone2EndHour
{
get => _zone2EndHour.Value;
set => _zone2EndHour.Value = value;
}

/// <summary>
/// Fast EMA length for MACD.
/// </summary>
public int FastPeriod
{
get => _fastPeriod.Value;
set => _fastPeriod.Value = value;
}

/// <summary>
/// Slow EMA length for MACD.
/// </summary>
public int SlowPeriod
{
get => _slowPeriod.Value;
set => _slowPeriod.Value = value;
}

/// <summary>
/// Signal moving average length.
/// </summary>
public int SignalPeriod
{
get => _signalPeriod.Value;
set => _signalPeriod.Value = value;
}

/// <summary>
/// MACD shift applied when reading indicator values.
/// </summary>
public int MacdShift
{
get => _macdShift.Value;
set => _macdShift.Value = value;
}

/// <summary>
/// Enable MACD zero-line crossover signals.
/// </summary>
public bool UseZeroCross
{
get => _useZeroCross.Value;
set => _useZeroCross.Value = value;
}

/// <summary>
/// Enable MACD versus signal crossovers.
/// </summary>
public bool UseSignalCross
{
get => _useSignalCross.Value;
set => _useSignalCross.Value = value;
}

/// <summary>
/// Allow positions to be flipped without closing opposite exposure first.
/// </summary>
public bool AllowHedging
{
get => _allowHedging.Value;
set => _allowHedging.Value = value;
}

/// <summary>
/// Automatically close the opposite exposure when an entry signal appears.
/// </summary>
public bool CloseOnReverseSignal
{
get => _closeOnReverseSignal.Value;
set => _closeOnReverseSignal.Value = value;
}

/// <summary>
/// Enable take-profit target management.
/// </summary>
public bool UseTakeProfit
{
get => _useTakeProfit.Value;
set => _useTakeProfit.Value = value;
}

/// <summary>
/// Take-profit distance expressed in pips.
/// </summary>
public decimal TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

/// <summary>
/// Enable stop-loss management.
/// </summary>
public bool UseStopLoss
{
get => _useStopLoss.Value;
set => _useStopLoss.Value = value;
}

/// <summary>
/// Stop-loss distance expressed in pips.
/// </summary>
public decimal StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

/// <summary>
/// Enable trailing stop logic.
/// </summary>
public bool UseTrailingStop
{
get => _useTrailingStop.Value;
set => _useTrailingStop.Value = value;
}

/// <summary>
/// Distance kept by the trailing stop in pips.
/// </summary>
public decimal TrailingPips
{
get => _trailingPips.Value;
set => _trailingPips.Value = value;
}

/// <summary>
/// Additional cushion in pips before trailing activates.
/// </summary>
public decimal TrailingCushionPips
{
get => _trailingCushionPips.Value;
set => _trailingCushionPips.Value = value;
}

/// <summary>
/// Enable breakeven stop adjustment.
/// </summary>
public bool UseRiskFree
{
get => _useRiskFree.Value;
set => _useRiskFree.Value = value;
}

/// <summary>
/// Profit required before moving the stop to entry.
/// </summary>
public decimal RiskFreePips
{
get => _riskFreePips.Value;
set => _riskFreePips.Value = value;
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

_macd = null;
_macdCurrent = null;
_macdPrevious = null;
_macdPrevPrev = null;
_signalCurrent = null;
_signalPrevious = null;
_signalPrevPrev = null;

ResetLong();
ResetShort();

_pipSize = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

Volume = TradeVolume;
_pipSize = CalculatePipSize();

_macd = new MovingAverageConvergenceDivergenceSignal
{
Macd =
{
ShortMa = { Length = FastPeriod },
LongMa = { Length = SlowPeriod },
},
SignalMa = { Length = SignalPeriod }
};

var subscription = SubscribeCandles(CandleType);
subscription
.BindEx(_macd, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _macd);
DrawOwnTrades(area);
}
}

private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
{
if (candle.State != CandleStates.Finished)
return;

if (_macd is null)
return;

if (!UpdateMacdHistory(macdValue))
return;

if (_pipSize <= 0m)
_pipSize = CalculatePipSize();

ManageExistingPosition(candle);

if (!IsFormedAndOnlineAndAllowTrading())
return;

if (UseTimeFilter && !IsWithinTradingWindow(candle.CloseTime))
return;

if (!TryGetShiftedValues(out var macdNow, out var macdPrev, out var signalNow, out var signalPrev))
return;

var histogramNow = macdNow - signalNow;
var histogramPrev = macdPrev - signalPrev;

var buySignal = (UseZeroCross && macdNow > 0m && macdPrev < 0m) ||
(UseSignalCross && histogramNow > 0m && histogramPrev < 0m);
var sellSignal = (UseZeroCross && macdNow < 0m && macdPrev > 0m) ||
(UseSignalCross && histogramNow < 0m && histogramPrev > 0m);

if (!buySignal && !sellSignal)
return;

if (buySignal)
TryEnterLong(candle);

if (sellSignal)
TryEnterShort(candle);
}

private bool UpdateMacdHistory(IIndicatorValue value)
{
if (value is not MovingAverageConvergenceDivergenceSignalValue macdTyped)
return false;

if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
return false;

_macdPrevPrev = _macdPrevious;
_macdPrevious = _macdCurrent;
_macdCurrent = macd;

_signalPrevPrev = _signalPrevious;
_signalPrevious = _signalCurrent;
_signalCurrent = signal;

return true;
}

private void ManageExistingPosition(ICandleMessage candle)
{
if (Position > 0m)
{
HandleLongPosition(candle);
return;
}

if (Position < 0m)
{
HandleShortPosition(candle);
return;
}

ResetLong();
ResetShort();
}

private void HandleLongPosition(ICandleMessage candle)
{
if (_longEntryPrice is not decimal entryPrice)
return;

UpdateLongTrailing(candle, entryPrice);
ApplyLongBreakeven(candle, entryPrice);

if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
{
SellMarket(Position);
ResetLong();
return;
}

if (_longTakeProfitPrice is decimal take && candle.HighPrice >= take)
{
SellMarket(Position);
ResetLong();
}
}

private void HandleShortPosition(ICandleMessage candle)
{
if (_shortEntryPrice is not decimal entryPrice)
return;

UpdateShortTrailing(candle, entryPrice);
ApplyShortBreakeven(candle, entryPrice);

if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
{
BuyMarket(-Position);
ResetShort();
return;
}

if (_shortTakeProfitPrice is decimal take && candle.LowPrice <= take)
{
BuyMarket(-Position);
ResetShort();
}
}

private void TryEnterLong(ICandleMessage candle)
{
if (!AllowHedging && Position < 0m)
{
if (CloseOnReverseSignal)
{
BuyMarket(-Position);
ResetShort();
}
else
{
return;
}
}

var currentLong = Position > 0m ? Position : 0m;
var remaining = MaxPositionVolume - currentLong;
if (remaining <= 0m)
return;

var volume = Math.Min(TradeVolume, remaining);
if (volume <= 0m)
return;

BuyMarket(volume);

var price = candle.ClosePrice;
if (_longEntryPrice is decimal existingPrice && currentLong > 0m)
{
var newVolume = currentLong + volume;
_longEntryPrice = ((existingPrice * currentLong) + (price * volume)) / newVolume;
}
else
{
_longEntryPrice = price;
}

SetupLongProtection();
}

private void TryEnterShort(ICandleMessage candle)
{
if (!AllowHedging && Position > 0m)
{
if (CloseOnReverseSignal)
{
SellMarket(Position);
ResetLong();
}
else
{
return;
}
}

var currentShort = Position < 0m ? -Position : 0m;
var remaining = MaxPositionVolume - currentShort;
if (remaining <= 0m)
return;

var volume = Math.Min(TradeVolume, remaining);
if (volume <= 0m)
return;

SellMarket(volume);

var price = candle.ClosePrice;
if (_shortEntryPrice is decimal existingPrice && currentShort > 0m)
{
var newVolume = currentShort + volume;
_shortEntryPrice = ((existingPrice * currentShort) + (price * volume)) / newVolume;
}
else
{
_shortEntryPrice = price;
}

SetupShortProtection();
}

private void SetupLongProtection()
{
if (_longEntryPrice is not decimal entryPrice)
return;

if (!UseStopLoss || StopLossPips <= 0m || _pipSize <= 0m)
{
_longStopPrice = null;
}
else
{
_longStopPrice = entryPrice - ConvertPipsToPrice(StopLossPips);
}

if (!UseTakeProfit || TakeProfitPips <= 0m || _pipSize <= 0m)
{
_longTakeProfitPrice = null;
}
else
{
_longTakeProfitPrice = entryPrice + ConvertPipsToPrice(TakeProfitPips);
}
}

private void SetupShortProtection()
{
if (_shortEntryPrice is not decimal entryPrice)
return;

if (!UseStopLoss || StopLossPips <= 0m || _pipSize <= 0m)
{
_shortStopPrice = null;
}
else
{
_shortStopPrice = entryPrice + ConvertPipsToPrice(StopLossPips);
}

if (!UseTakeProfit || TakeProfitPips <= 0m || _pipSize <= 0m)
{
_shortTakeProfitPrice = null;
}
else
{
_shortTakeProfitPrice = entryPrice - ConvertPipsToPrice(TakeProfitPips);
}
}

private void UpdateLongTrailing(ICandleMessage candle, decimal entryPrice)
{
if (!UseTrailingStop || TrailingPips <= 0m || _pipSize <= 0m)
return;

var profitPips = (candle.ClosePrice - entryPrice) / _pipSize;
if (profitPips <= TrailingPips + TrailingCushionPips)
return;

var newStop = candle.ClosePrice - ConvertPipsToPrice(TrailingPips);
var minStop = entryPrice;

if (_longStopPrice is decimal currentStop)
{
if (newStop <= currentStop)
return;
minStop = Math.Max(minStop, currentStop);
}

_longStopPrice = Math.Max(newStop, minStop);
}

private void UpdateShortTrailing(ICandleMessage candle, decimal entryPrice)
{
if (!UseTrailingStop || TrailingPips <= 0m || _pipSize <= 0m)
return;

var profitPips = (entryPrice - candle.ClosePrice) / _pipSize;
if (profitPips <= TrailingPips + TrailingCushionPips)
return;

var newStop = candle.ClosePrice + ConvertPipsToPrice(TrailingPips);
var maxStop = entryPrice;

if (_shortStopPrice is decimal currentStop)
{
if (newStop >= currentStop)
return;
maxStop = Math.Min(maxStop, currentStop);
}

_shortStopPrice = Math.Min(newStop, maxStop);
}

private void ApplyLongBreakeven(ICandleMessage candle, decimal entryPrice)
{
if (!UseRiskFree || RiskFreePips <= 0m || _pipSize <= 0m)
return;

var profitPips = (candle.ClosePrice - entryPrice) / _pipSize;
if (profitPips < RiskFreePips)
return;

if (_longStopPrice is decimal stop && stop >= entryPrice)
return;

_longStopPrice = entryPrice;
}

private void ApplyShortBreakeven(ICandleMessage candle, decimal entryPrice)
{
if (!UseRiskFree || RiskFreePips <= 0m || _pipSize <= 0m)
return;

var profitPips = (entryPrice - candle.ClosePrice) / _pipSize;
if (profitPips < RiskFreePips)
return;

if (_shortStopPrice is decimal stop && stop <= entryPrice)
return;

_shortStopPrice = entryPrice;
}

private bool TryGetShiftedValues(out decimal macdNow, out decimal macdPrev, out decimal signalNow, out decimal signalPrev)
{
macdNow = 0m;
macdPrev = 0m;
signalNow = 0m;
signalPrev = 0m;

if (MacdShift == 0)
{
if (_macdCurrent is not decimal current || _macdPrevious is not decimal previous ||
_signalCurrent is not decimal sigCurrent || _signalPrevious is not decimal sigPrevious)
return false;

macdNow = current;
macdPrev = previous;
signalNow = sigCurrent;
signalPrev = sigPrevious;
return true;
}

if (_macdPrevious is not decimal shiftedCurrent || _macdPrevPrev is not decimal shiftedPrev ||
_signalPrevious is not decimal shiftedSignalCurrent || _signalPrevPrev is not decimal shiftedSignalPrev)
return false;

macdNow = shiftedCurrent;
macdPrev = shiftedPrev;
signalNow = shiftedSignalCurrent;
signalPrev = shiftedSignalPrev;
return true;
}

private bool IsWithinTradingWindow(DateTimeOffset time)
{
return IsHourInWindow(time.Hour, Zone1StartHour, Zone1EndHour) ||
IsHourInWindow(time.Hour, Zone2StartHour, Zone2EndHour);
}

private static bool IsHourInWindow(int hour, int start, int end)
{
if (start == end)
return true;

if (start < end)
return hour >= start && hour <= end;

return hour >= start || hour <= end;
}

private decimal ConvertPipsToPrice(decimal pips)
{
return pips * _pipSize;
}

private decimal CalculatePipSize()
{
var security = Security;
if (security?.PriceStep is decimal step && step > 0m)
{
return step;
}

return 0.0001m;
}

private void ResetLong()
{
_longEntryPrice = null;
_longStopPrice = null;
_longTakeProfitPrice = null;
}

private void ResetShort()
{
_shortEntryPrice = null;
_shortStopPrice = null;
_shortTakeProfitPrice = null;
}
}

