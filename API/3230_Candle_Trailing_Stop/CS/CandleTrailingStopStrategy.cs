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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Candle based trailing stop strategy converted from the MetaTrader "Candle Trailing Stop" expert.
/// </summary>
public class CandleTrailingStopStrategy : Strategy
{
private readonly StrategyParam<int> _maxTrades;
private readonly StrategyParam<int> _fastHigherLength;
private readonly StrategyParam<int> _middleHigherLength;
private readonly StrategyParam<int> _slowHigherLength;
private readonly StrategyParam<int> _fastCurrentLength;
private readonly StrategyParam<int> _middleCurrentLength;
private readonly StrategyParam<int> _slowCurrentLength;
private readonly StrategyParam<int> _momentumPeriod;
private readonly StrategyParam<decimal> _momentumBuyThreshold;
private readonly StrategyParam<decimal> _momentumSellThreshold;
private readonly StrategyParam<int> _macdFastLength;
private readonly StrategyParam<int> _macdSlowLength;
private readonly StrategyParam<int> _macdSignalLength;
private readonly StrategyParam<decimal> _stopLossPips;
private readonly StrategyParam<decimal> _takeProfitPips;
private readonly StrategyParam<bool> _useMoveToBreakEven;
private readonly StrategyParam<decimal> _breakEvenTriggerPips;
private readonly StrategyParam<decimal> _breakEvenOffsetPips;
private readonly StrategyParam<bool> _useCandleTrail;
private readonly StrategyParam<int> _candleTrailLength;
private readonly StrategyParam<decimal> _padAmountPips;
private readonly StrategyParam<decimal> _trailTriggerPips;
private readonly StrategyParam<decimal> _trailAmountPips;
private readonly StrategyParam<bool> _useMoneyTakeProfit;
private readonly StrategyParam<decimal> _moneyTakeProfit;
private readonly StrategyParam<bool> _usePercentTakeProfit;
private readonly StrategyParam<decimal> _percentTakeProfit;
private readonly StrategyParam<bool> _enableMoneyTrailing;
private readonly StrategyParam<decimal> _moneyTrailTarget;
private readonly StrategyParam<decimal> _moneyTrailStop;
private readonly StrategyParam<bool> _useEquityStop;
private readonly StrategyParam<decimal> _equityRiskPercent;
private readonly StrategyParam<DataType> _currentCandleType;
private readonly StrategyParam<DataType> _higherCandleType;
private readonly StrategyParam<DataType> _macdCandleType;

private WeightedMovingAverage _currentFast = null!;
private WeightedMovingAverage _currentMiddle = null!;
private WeightedMovingAverage _currentSlow = null!;
private WeightedMovingAverage _higherFast = null!;
private WeightedMovingAverage _higherMiddle = null!;
private WeightedMovingAverage _higherSlow = null!;
private Momentum _momentum = null!;
private MovingAverageConvergenceDivergenceSignal _macd = null!;

private readonly Queue<decimal> _momentumBuffer = new();
private readonly Queue<decimal> _recentLows = new();
private readonly Queue<decimal> _recentHighs = new();

private decimal? _currentFastValue;
private decimal? _currentMiddleValue;
private decimal? _currentSlowValue;
private decimal? _higherFastValue;
private decimal? _higherMiddleValue;
private decimal? _higherSlowValue;
private decimal? _macdLineValue;
private decimal? _macdSignalValue;

private ICandleMessage _previousCurrentCandle;

private decimal? _longStop;
private decimal? _shortStop;

private decimal _tickSize;
private decimal _moneyTrailPeak;
private decimal _equityPeak;
private decimal _initialEquity;

/// <summary>
/// Initializes a new instance of the <see cref="CandleTrailingStopStrategy"/> class.
/// </summary>
public CandleTrailingStopStrategy()
{

_maxTrades = Param(nameof(MaxTrades), 10)
.SetGreaterThanZero()
.SetCanOptimize(true)
.SetDisplay("Max trades", "Maximum aggregated position expressed in trade count", "Trading");

_fastHigherLength = Param(nameof(FastHigherLength), 9)
.SetGreaterThanZero()
.SetCanOptimize(true)
.SetDisplay("Higher fast LWMA", "Length of the fast LWMA calculated on the higher timeframe", "Indicators");

_middleHigherLength = Param(nameof(MiddleHigherLength), 20)
.SetGreaterThanZero()
.SetCanOptimize(true)
.SetDisplay("Higher middle LWMA", "Length of the middle LWMA calculated on the higher timeframe", "Indicators");

_slowHigherLength = Param(nameof(SlowHigherLength), 52)
.SetGreaterThanZero()
.SetCanOptimize(true)
.SetDisplay("Higher slow LWMA", "Length of the slow LWMA calculated on the higher timeframe", "Indicators");

_fastCurrentLength = Param(nameof(FastCurrentLength), 9)
.SetGreaterThanZero()
.SetDisplay("Current fast LWMA", "Length of the fast LWMA calculated on the trading timeframe", "Indicators");

_middleCurrentLength = Param(nameof(MiddleCurrentLength), 20)
.SetGreaterThanZero()
.SetDisplay("Current middle LWMA", "Length of the middle LWMA calculated on the trading timeframe", "Indicators");

_slowCurrentLength = Param(nameof(SlowCurrentLength), 52)
.SetGreaterThanZero()
.SetDisplay("Current slow LWMA", "Length of the slow LWMA calculated on the trading timeframe", "Indicators");

_momentumPeriod = Param(nameof(MomentumPeriod), 14)
.SetGreaterThanZero()
.SetDisplay("Momentum period", "Period of the momentum indicator on the higher timeframe", "Indicators");

_momentumBuyThreshold = Param(nameof(MomentumBuyThreshold), 0.3m)
.SetGreaterThanZero()
.SetDisplay("Momentum buy threshold", "Maximum deviation from 100 required for long trades", "Filters");

_momentumSellThreshold = Param(nameof(MomentumSellThreshold), 0.3m)
.SetGreaterThanZero()
.SetDisplay("Momentum sell threshold", "Maximum deviation from 100 required for short trades", "Filters");

_macdFastLength = Param(nameof(MacdFastLength), 12)
.SetGreaterThanZero()
.SetDisplay("MACD fast length", "Fast EMA length used by the MACD filter", "Indicators");

_macdSlowLength = Param(nameof(MacdSlowLength), 26)
.SetGreaterThanZero()
.SetDisplay("MACD slow length", "Slow EMA length used by the MACD filter", "Indicators");

_macdSignalLength = Param(nameof(MacdSignalLength), 9)
.SetGreaterThanZero()
.SetDisplay("MACD signal length", "Signal EMA length used by the MACD filter", "Indicators");

_stopLossPips = Param(nameof(StopLossPips), 20m)
.SetGreaterThanZero()
.SetDisplay("Stop loss (pips)", "Distance from entry to protective stop", "Risk");

_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
.SetGreaterThanZero()
.SetDisplay("Take profit (pips)", "Distance from entry to take profit", "Risk");

_useMoveToBreakEven = Param(nameof(UseMoveToBreakEven), true)
.SetDisplay("Move to breakeven", "Enable automatic break-even adjustment", "Risk");

_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 30m)
.SetGreaterThanZero()
.SetDisplay("Break-even trigger", "Unrealized profit required before moving the stop", "Risk");

_breakEvenOffsetPips = Param(nameof(BreakEvenOffsetPips), 30m)
.SetGreaterThanZero()
.SetDisplay("Break-even offset", "Additional distance added to the break-even stop", "Risk");

_useCandleTrail = Param(nameof(UseCandleTrail), true)
.SetDisplay("Use candle trail", "Trail stops using recent candle extremes", "Risk");

_candleTrailLength = Param(nameof(CandleTrailLength), 3)
.SetGreaterThanZero()
.SetDisplay("Candle trail length", "Number of completed candles used for trailing", "Risk");

_padAmountPips = Param(nameof(PadAmountPips), 10m)
.SetGreaterThanZero()
.SetDisplay("Padding (pips)", "Extra distance added to candle trailing levels", "Risk");

_trailTriggerPips = Param(nameof(TrailTriggerPips), 40m)
.SetGreaterThanZero()
.SetDisplay("Trail trigger", "Profit in pips required before price trailing starts", "Risk");

_trailAmountPips = Param(nameof(TrailAmountPips), 40m)
.SetGreaterThanZero()
.SetDisplay("Trail amount", "Distance maintained between price and trailing stop", "Risk");

_useMoneyTakeProfit = Param(nameof(UseMoneyTakeProfit), false)
.SetDisplay("Use money take profit", "Close all positions once the floating profit reaches the specified value", "Risk");

_moneyTakeProfit = Param(nameof(MoneyTakeProfit), 40m)
.SetGreaterThanZero()
.SetDisplay("Money take profit", "Profit target in account currency", "Risk");

_usePercentTakeProfit = Param(nameof(UsePercentTakeProfit), false)
.SetDisplay("Use percent take profit", "Close all positions once profit exceeds the percentage of equity", "Risk");

_percentTakeProfit = Param(nameof(PercentTakeProfit), 10m)
.SetGreaterThanZero()
.SetDisplay("Percent take profit", "Profit target expressed as equity percentage", "Risk");

_enableMoneyTrailing = Param(nameof(EnableMoneyTrailing), true)
.SetDisplay("Enable money trailing", "Protect floating profit using a trailing stop on equity", "Risk");

_moneyTrailTarget = Param(nameof(MoneyTrailTarget), 40m)
.SetGreaterThanZero()
.SetDisplay("Money trail target", "Profit level that activates the money trailing logic", "Risk");

_moneyTrailStop = Param(nameof(MoneyTrailStop), 10m)
.SetGreaterThanZero()
.SetDisplay("Money trail stop", "Maximum allowed giveback after the target is reached", "Risk");

_useEquityStop = Param(nameof(UseEquityStop), true)
.SetDisplay("Use equity stop", "Protect accumulated equity using a drawdown limit", "Risk");

_equityRiskPercent = Param(nameof(EquityRiskPercent), 1m)
.SetGreaterThanZero()
.SetDisplay("Equity risk percent", "Maximum drawdown from the equity peak", "Risk");

_currentCandleType = Param(nameof(CurrentCandleType), TimeSpan.FromMinutes(5).TimeFrame())
.SetDisplay("Current timeframe", "Primary candle series used for trading decisions", "Data");

_higherCandleType = Param(nameof(HigherCandleType), TimeSpan.FromMinutes(30).TimeFrame())
.SetDisplay("Higher timeframe", "Secondary candle series used for trend and momentum filters", "Data");

_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
.SetDisplay("MACD timeframe", "Timeframe used for the MACD confirmation filter", "Data");
}


/// <summary>
/// Maximum number of trades that can be open simultaneously.
/// </summary>
public int MaxTrades
{
get => _maxTrades.Value;
set => _maxTrades.Value = value;
}

/// <summary>
/// Higher timeframe fast LWMA length.
/// </summary>
public int FastHigherLength
{
get => _fastHigherLength.Value;
set => _fastHigherLength.Value = value;
}

/// <summary>
/// Higher timeframe middle LWMA length.
/// </summary>
public int MiddleHigherLength
{
get => _middleHigherLength.Value;
set => _middleHigherLength.Value = value;
}

/// <summary>
/// Higher timeframe slow LWMA length.
/// </summary>
public int SlowHigherLength
{
get => _slowHigherLength.Value;
set => _slowHigherLength.Value = value;
}

/// <summary>
/// Current timeframe fast LWMA length.
/// </summary>
public int FastCurrentLength
{
get => _fastCurrentLength.Value;
set => _fastCurrentLength.Value = value;
}

/// <summary>
/// Current timeframe middle LWMA length.
/// </summary>
public int MiddleCurrentLength
{
get => _middleCurrentLength.Value;
set => _middleCurrentLength.Value = value;
}

/// <summary>
/// Current timeframe slow LWMA length.
/// </summary>
public int SlowCurrentLength
{
get => _slowCurrentLength.Value;
set => _slowCurrentLength.Value = value;
}

/// <summary>
/// Momentum indicator period.
/// </summary>
public int MomentumPeriod
{
get => _momentumPeriod.Value;
set => _momentumPeriod.Value = value;
}

/// <summary>
/// Maximum deviation allowed for long momentum entries.
/// </summary>
public decimal MomentumBuyThreshold
{
get => _momentumBuyThreshold.Value;
set => _momentumBuyThreshold.Value = value;
}

/// <summary>
/// Maximum deviation allowed for short momentum entries.
/// </summary>
public decimal MomentumSellThreshold
{
get => _momentumSellThreshold.Value;
set => _momentumSellThreshold.Value = value;
}

/// <summary>
/// Fast length used by MACD.
/// </summary>
public int MacdFastLength
{
get => _macdFastLength.Value;
set => _macdFastLength.Value = value;
}

/// <summary>
/// Slow length used by MACD.
/// </summary>
public int MacdSlowLength
{
get => _macdSlowLength.Value;
set => _macdSlowLength.Value = value;
}

/// <summary>
/// Signal length used by MACD.
/// </summary>
public int MacdSignalLength
{
get => _macdSignalLength.Value;
set => _macdSignalLength.Value = value;
}

/// <summary>
/// Stop loss distance expressed in pips.
/// </summary>
public decimal StopLossPips
{
get => _stopLossPips.Value;
set => _stopLossPips.Value = value;
}

/// <summary>
/// Take profit distance expressed in pips.
/// </summary>
public decimal TakeProfitPips
{
get => _takeProfitPips.Value;
set => _takeProfitPips.Value = value;
}

/// <summary>
/// Enables automatic break-even adjustments.
/// </summary>
public bool UseMoveToBreakEven
{
get => _useMoveToBreakEven.Value;
set => _useMoveToBreakEven.Value = value;
}

/// <summary>
/// Profit in pips required before the break-even logic activates.
/// </summary>
public decimal BreakEvenTriggerPips
{
get => _breakEvenTriggerPips.Value;
set => _breakEvenTriggerPips.Value = value;
}

/// <summary>
/// Additional offset applied to the break-even stop.
/// </summary>
public decimal BreakEvenOffsetPips
{
get => _breakEvenOffsetPips.Value;
set => _breakEvenOffsetPips.Value = value;
}

/// <summary>
/// Enables candle trailing logic.
/// </summary>
public bool UseCandleTrail
{
get => _useCandleTrail.Value;
set => _useCandleTrail.Value = value;
}

/// <summary>
/// Number of candles used to calculate trailing levels.
/// </summary>
public int CandleTrailLength
{
get => _candleTrailLength.Value;
set => _candleTrailLength.Value = value;
}

/// <summary>
/// Additional padding added to candle trailing stops.
/// </summary>
public decimal PadAmountPips
{
get => _padAmountPips.Value;
set => _padAmountPips.Value = value;
}

/// <summary>
/// Profit required before classic trailing starts when candle trailing is disabled.
/// </summary>
public decimal TrailTriggerPips
{
get => _trailTriggerPips.Value;
set => _trailTriggerPips.Value = value;
}

/// <summary>
/// Distance maintained by the classic trailing stop.
/// </summary>
public decimal TrailAmountPips
{
get => _trailAmountPips.Value;
set => _trailAmountPips.Value = value;
}

/// <summary>
/// Enable the monetary take profit.
/// </summary>
public bool UseMoneyTakeProfit
{
get => _useMoneyTakeProfit.Value;
set => _useMoneyTakeProfit.Value = value;
}

/// <summary>
/// Monetary take profit value.
/// </summary>
public decimal MoneyTakeProfit
{
get => _moneyTakeProfit.Value;
set => _moneyTakeProfit.Value = value;
}

/// <summary>
/// Enable the percent based take profit.
/// </summary>
public bool UsePercentTakeProfit
{
get => _usePercentTakeProfit.Value;
set => _usePercentTakeProfit.Value = value;
}

/// <summary>
/// Percent take profit value.
/// </summary>
public decimal PercentTakeProfit
{
get => _percentTakeProfit.Value;
set => _percentTakeProfit.Value = value;
}

/// <summary>
/// Enable the trailing of floating profit.
/// </summary>
public bool EnableMoneyTrailing
{
get => _enableMoneyTrailing.Value;
set => _enableMoneyTrailing.Value = value;
}

/// <summary>
/// Profit level that activates the money trailing logic.
/// </summary>
public decimal MoneyTrailTarget
{
get => _moneyTrailTarget.Value;
set => _moneyTrailTarget.Value = value;
}

/// <summary>
/// Maximum allowed drawdown from the profit peak.
/// </summary>
public decimal MoneyTrailStop
{
get => _moneyTrailStop.Value;
set => _moneyTrailStop.Value = value;
}

/// <summary>
/// Enable the equity stop.
/// </summary>
public bool UseEquityStop
{
get => _useEquityStop.Value;
set => _useEquityStop.Value = value;
}

/// <summary>
/// Maximum allowed equity drawdown percentage.
/// </summary>
public decimal EquityRiskPercent
{
get => _equityRiskPercent.Value;
set => _equityRiskPercent.Value = value;
}

/// <summary>
/// Trading timeframe.
/// </summary>
public DataType CurrentCandleType
{
get => _currentCandleType.Value;
set => _currentCandleType.Value = value;
}

/// <summary>
/// Higher timeframe used for filters.
/// </summary>
public DataType HigherCandleType
{
get => _higherCandleType.Value;
set => _higherCandleType.Value = value;
}

/// <summary>
/// MACD confirmation timeframe.
/// </summary>
public DataType MacdCandleType
{
get => _macdCandleType.Value;
set => _macdCandleType.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return new[]
{
(Security, CurrentCandleType),
(Security, HigherCandleType),
(Security, MacdCandleType)
};
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_momentumBuffer.Clear();
_recentLows.Clear();
_recentHighs.Clear();
_currentFastValue = null;
_currentMiddleValue = null;
_currentSlowValue = null;
_higherFastValue = null;
_higherMiddleValue = null;
_higherSlowValue = null;
_macdLineValue = null;
_macdSignalValue = null;
_previousCurrentCandle = null;
_longStop = null;
_shortStop = null;
_moneyTrailPeak = 0m;
_equityPeak = 0m;
_initialEquity = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_tickSize = Security?.PriceStep ?? 0.0001m;

_currentFast = new WeightedMovingAverage { Length = FastCurrentLength };
_currentMiddle = new WeightedMovingAverage { Length = MiddleCurrentLength };
_currentSlow = new WeightedMovingAverage { Length = SlowCurrentLength };

_higherFast = new WeightedMovingAverage { Length = FastHigherLength };
_higherMiddle = new WeightedMovingAverage { Length = MiddleHigherLength };
_higherSlow = new WeightedMovingAverage { Length = SlowHigherLength };

_momentum = new Momentum { Length = MomentumPeriod };

_macd = new MovingAverageConvergenceDivergenceSignal
{
Fast = { Length = MacdFastLength },
Slow = { Length = MacdSlowLength },
Signal = { Length = MacdSignalLength }
};

var currentSubscription = SubscribeCandles(CurrentCandleType);
currentSubscription.BindEx(_currentFast, _currentMiddle, _currentSlow, ProcessCurrentCandle).Start();

var higherSubscription = SubscribeCandles(HigherCandleType);
higherSubscription.BindEx(_higherFast, _higherMiddle, _higherSlow, _momentum, ProcessHigherCandle).Start();

var macdSubscription = SubscribeCandles(MacdCandleType);
macdSubscription.BindEx(_macd, ProcessMacdCandle).Start();

_initialEquity = GetPortfolioValue();
_equityPeak = _initialEquity;

StartProtection();
}

private void ProcessCurrentCandle(
ICandleMessage candle,
IIndicatorValue fastValue,
IIndicatorValue middleValue,
IIndicatorValue slowValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!fastValue.IsFinal || !middleValue.IsFinal || !slowValue.IsFinal)
return;

_currentFastValue = fastValue.GetValue<decimal>();
_currentMiddleValue = middleValue.GetValue<decimal>();
_currentSlowValue = slowValue.GetValue<decimal>();

UpdateRecentExtremes(candle);
ManagePositions(candle);
EvaluateEntries(candle);

_previousCurrentCandle = candle;
}

private void ProcessHigherCandle(
ICandleMessage candle,
IIndicatorValue fastValue,
IIndicatorValue middleValue,
IIndicatorValue slowValue,
IIndicatorValue momentumValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!fastValue.IsFinal || !middleValue.IsFinal || !slowValue.IsFinal || !momentumValue.IsFinal)
return;

_higherFastValue = fastValue.GetValue<decimal>();
_higherMiddleValue = middleValue.GetValue<decimal>();
_higherSlowValue = slowValue.GetValue<decimal>();

var momentum = momentumValue.GetValue<decimal>();
var deviation = Math.Abs(100m - momentum);

_momentumBuffer.Enqueue(deviation);
while (_momentumBuffer.Count > 3)
_momentumBuffer.Dequeue();

EvaluateEntries(_previousCurrentCandle);
}

private void ProcessMacdCandle(ICandleMessage candle, IIndicatorValue macdValue)
{
if (candle.State != CandleStates.Finished)
return;

if (!macdValue.IsFinal)
return;

if (macdValue is not MovingAverageConvergenceDivergenceSignalValue macd)
return;

if (macd.Macd is not decimal macdLine || macd.Signal is not decimal signalLine)
return;

_macdLineValue = macdLine;
_macdSignalValue = signalLine;

EvaluateEntries(_previousCurrentCandle);
}

private void EvaluateEntries(ICandleMessage candle)
{
if (candle is null || !IsFormedAndOnlineAndAllowTrading())
return;

if (_currentFastValue is null || _currentMiddleValue is null || _currentSlowValue is null)
return;

if (_higherFastValue is null || _higherMiddleValue is null || _higherSlowValue is null)
return;

if (_momentumBuffer.Count < 3)
return;

if (_macdLineValue is null || _macdSignalValue is null)
return;

if (_previousCurrentCandle is null)
return;

var positionLimit = Volume * MaxTrades;

var fastCurrent = _currentFastValue.Value;
var middleCurrent = _currentMiddleValue.Value;
var slowCurrent = _currentSlowValue.Value;

var fastHigher = _higherFastValue.Value;
var middleHigher = _higherMiddleValue.Value;
var slowHigher = _higherSlowValue.Value;

var momentumOkForBuy = _momentumBuffer.Any(v => v <= MomentumBuyThreshold);
var momentumOkForSell = _momentumBuffer.Any(v => v <= MomentumSellThreshold);

var macdBullish = _macdLineValue > _macdSignalValue;
var macdBearish = _macdLineValue < _macdSignalValue;

var previousLow = _previousCurrentCandle.LowPrice;
var previousHigh = _previousCurrentCandle.HighPrice;
var close = candle.ClosePrice;

if (Position <= 0 && Math.Abs(Position) < positionLimit)
{
var buyCondition =
fastHigher > middleHigher && middleHigher > slowHigher &&
fastCurrent > middleCurrent && middleCurrent > slowCurrent &&
previousLow <= fastCurrent && close > fastCurrent &&
momentumOkForBuy &&
macdBullish;

if (buyCondition)
{
BuyMarket(Volume);
SetInitialStops(Sides.Buy, candle);
}
}

if (Position >= 0 && Math.Abs(Position) < positionLimit)
{
var sellCondition =
fastHigher < middleHigher && middleHigher < slowHigher &&
fastCurrent < middleCurrent && middleCurrent < slowCurrent &&
previousHigh >= fastCurrent && close < fastCurrent &&
momentumOkForSell &&
macdBearish;

if (sellCondition)
{
SellMarket(Volume);
SetInitialStops(Sides.Sell, candle);
}
}
}

private void SetInitialStops(Sides direction, ICandleMessage candle)
{
var stepValue = GetStepValue(StopLossPips);
var takeProfit = GetStepValue(TakeProfitPips);

if (direction == Sides.Buy)
{
_longStop = candle.ClosePrice - stepValue;
if (takeProfit > 0m)
RegisterOrder(CreateOrder(Sides.Sell, candle.ClosePrice + takeProfit, Volume));
}
else
{
_shortStop = candle.ClosePrice + stepValue;
if (takeProfit > 0m)
RegisterOrder(CreateOrder(Sides.Buy, candle.ClosePrice - takeProfit, Volume));
}
}

private void ManagePositions(ICandleMessage candle)
{
UpdateMoneyManagement(candle);

if (Position > 0)
{
UpdateLongStops(candle);

if (_longStop.HasValue && candle.LowPrice <= _longStop)
{
SellMarket(Position);
_longStop = null;
}
}
else if (Position < 0)
{
UpdateShortStops(candle);

if (_shortStop.HasValue && candle.HighPrice >= _shortStop)
{
BuyMarket(Math.Abs(Position));
_shortStop = null;
}
}
else
{
_longStop = null;
_shortStop = null;
}
}

private void UpdateLongStops(ICandleMessage candle)
{
var entryPrice = Position.AveragePrice;
if (entryPrice == 0m)
return;

if (UseMoveToBreakEven)
{
var trigger = entryPrice + GetStepValue(BreakEvenTriggerPips);
if (candle.HighPrice >= trigger)
{
var stopCandidate = entryPrice + GetStepValue(BreakEvenOffsetPips);
_longStop = _longStop is null ? stopCandidate : Math.Max(_longStop.Value, stopCandidate);
}
}

if (UseCandleTrail && _recentLows.Count >= CandleTrailLength)
{
var trailingLow = _recentLows.Min();
var stopCandidate = trailingLow - GetStepValue(PadAmountPips);
_longStop = _longStop is null ? stopCandidate : Math.Max(_longStop.Value, stopCandidate);
}
else if (!UseCandleTrail)
{
var trigger = entryPrice + GetStepValue(TrailTriggerPips);
if (candle.ClosePrice >= trigger)
{
var stopCandidate = candle.ClosePrice - GetStepValue(TrailAmountPips);
_longStop = _longStop is null ? stopCandidate : Math.Max(_longStop.Value, stopCandidate);
}
}
}

private void UpdateShortStops(ICandleMessage candle)
{
var entryPrice = Position.AveragePrice;
if (entryPrice == 0m)
return;

if (UseMoveToBreakEven)
{
var trigger = entryPrice - GetStepValue(BreakEvenTriggerPips);
if (candle.LowPrice <= trigger)
{
var stopCandidate = entryPrice - GetStepValue(BreakEvenOffsetPips);
_shortStop = _shortStop is null ? stopCandidate : Math.Min(_shortStop.Value, stopCandidate);
}
}

if (UseCandleTrail && _recentHighs.Count >= CandleTrailLength)
{
var trailingHigh = _recentHighs.Max();
var stopCandidate = trailingHigh + GetStepValue(PadAmountPips);
_shortStop = _shortStop is null ? stopCandidate : Math.Min(_shortStop.Value, stopCandidate);
}
else if (!UseCandleTrail)
{
var trigger = entryPrice - GetStepValue(TrailTriggerPips);
if (candle.ClosePrice <= trigger)
{
var stopCandidate = candle.ClosePrice + GetStepValue(TrailAmountPips);
_shortStop = _shortStop is null ? stopCandidate : Math.Min(_shortStop.Value, stopCandidate);
}
}
}

private void UpdateMoneyManagement(ICandleMessage candle)
{
var unrealized = GetUnrealizedPnL(candle);

if (UseMoneyTakeProfit && MoneyTakeProfit > 0m && unrealized >= MoneyTakeProfit)
{
ClosePosition();
return;
}

if (UsePercentTakeProfit && PercentTakeProfit > 0m && _initialEquity > 0m)
{
var target = _initialEquity * PercentTakeProfit / 100m;
if (unrealized >= target)
{
ClosePosition();
return;
}
}

if (EnableMoneyTrailing && MoneyTrailTarget > 0m && MoneyTrailStop > 0m)
{
if (unrealized >= MoneyTrailTarget)
{
_moneyTrailPeak = Math.Max(_moneyTrailPeak, unrealized);
if (_moneyTrailPeak - unrealized >= MoneyTrailStop)
ClosePosition();
}
else
{
_moneyTrailPeak = 0m;
}
}

if (UseEquityStop && EquityRiskPercent > 0m)
{
var equity = GetPortfolioValue() + unrealized;
_equityPeak = Math.Max(_equityPeak, equity);

var drawdown = _equityPeak - equity;
var limit = _equityPeak * EquityRiskPercent / 100m;

if (drawdown >= limit && Position != 0)
ClosePosition();
}
}

private void UpdateRecentExtremes(ICandleMessage candle)
{
_recentLows.Enqueue(candle.LowPrice);
_recentHighs.Enqueue(candle.HighPrice);

while (_recentLows.Count > CandleTrailLength)
_recentLows.Dequeue();

while (_recentHighs.Count > CandleTrailLength)
_recentHighs.Dequeue();
}

private decimal GetStepValue(decimal pips)
{
var step = _tickSize == 0m ? 0.0001m : _tickSize;
return pips * step;
}

private decimal GetUnrealizedPnL(ICandleMessage candle)
{
if (Position == 0)
return 0m;

var entry = Position.AveragePrice;
if (entry == 0m)
return 0m;

var diff = candle.ClosePrice - entry;
return diff * Position;
}

private decimal GetPortfolioValue()
{
var portfolio = Portfolio;
if (portfolio?.CurrentValue > 0m)
return portfolio.CurrentValue;

if (portfolio?.BeginValue > 0m)
return portfolio.BeginValue;

return 0m;
}
}

