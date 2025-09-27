namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy converted from the "Tuyul Uncensored" MetaTrader expert advisor.
/// It watches ZigZag swings, aligns with an EMA trend filter, and places Fibonacci retracement limit orders.
/// </summary>
public class TuyulUncensoredStrategy : Strategy
{
private readonly StrategyParam<decimal> _volume;
private readonly StrategyParam<decimal> _takeProfitMultiplier;
private readonly StrategyParam<int> _zigZagDepth;
private readonly StrategyParam<decimal> _zigZagDeviation;
private readonly StrategyParam<int> _zigZagBackstep;
private readonly StrategyParam<int> _waitBars;
private readonly StrategyParam<int> _fastEmaPeriod;
private readonly StrategyParam<int> _slowEmaPeriod;
private readonly StrategyParam<bool> _allowMonday;
private readonly StrategyParam<bool> _allowTuesday;
private readonly StrategyParam<bool> _allowWednesday;
private readonly StrategyParam<bool> _allowThursday;
private readonly StrategyParam<bool> _allowFriday;
private readonly StrategyParam<DataType> _candleType;
private readonly StrategyParam<decimal> _fibLevel;

private readonly List<(DateTimeOffset Time, decimal Price)> _pivots = new();

private ZigZagIndicator _zigZag = null!;
private EMA _fastEma = null!;
private EMA _slowEma = null!;

private decimal _lastZigZagHigh;
private decimal _lastZigZagLow;

private decimal? _previousFast;
private decimal? _previousSlow;

private Order _pendingOrder;
private decimal? _plannedStop;
private decimal? _plannedTake;
private int _plannedDirection;
private int _barsSinceOrder;

private decimal? _activeStop;
private decimal? _activeTake;
private int _activeDirection;

/// <summary>
/// Initializes a new instance of the <see cref="TuyulUncensoredStrategy"/> class.
/// </summary>
public TuyulUncensoredStrategy()
{
_volume = Param(nameof(VolumePerTrade), 0.03m)
.SetDisplay("Volume", "Order volume per trade", "General")
.SetGreaterThanZero();

_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 1.2m)
.SetDisplay("TP Multiplier", "Take profit distance relative to stop loss", "Risk")
.SetGreaterThanZero();

_zigZagDepth = Param(nameof(ZigZagDepth), 12)
.SetDisplay("ZigZag Depth", "Number of bars to evaluate for swings", "ZigZag")
.SetGreaterThanZero();

_zigZagDeviation = Param(nameof(ZigZagDeviation), 5m)
.SetDisplay("ZigZag Deviation", "Minimum deviation in points to confirm a swing", "ZigZag")
.SetNotNegative();

_zigZagBackstep = Param(nameof(ZigZagBackstep), 3)
.SetDisplay("ZigZag Backstep", "Bars required between opposite pivots", "ZigZag")
.SetNotNegative();

_waitBars = Param(nameof(WaitBarsAfterSignal), 12)
.SetDisplay("Wait Bars", "Candles to keep the pending order before cancelling", "Trading")
.SetNotNegative();

_fastEmaPeriod = Param(nameof(FastEmaPeriod), 9)
.SetDisplay("Fast EMA", "Period of the fast EMA filter", "Trend")
.SetGreaterThanZero();

_slowEmaPeriod = Param(nameof(SlowEmaPeriod), 21)
.SetDisplay("Slow EMA", "Period of the slow EMA filter", "Trend")
.SetGreaterThanZero();

_allowMonday = Param(nameof(AllowMonday), true)
.SetDisplay("Allow Monday", "Enable trading on Monday", "Schedule");

_allowTuesday = Param(nameof(AllowTuesday), true)
.SetDisplay("Allow Tuesday", "Enable trading on Tuesday", "Schedule");

_allowWednesday = Param(nameof(AllowWednesday), true)
.SetDisplay("Allow Wednesday", "Enable trading on Wednesday", "Schedule");

_allowThursday = Param(nameof(AllowThursday), true)
.SetDisplay("Allow Thursday", "Enable trading on Thursday", "Schedule");

_allowFriday = Param(nameof(AllowFriday), true)
.SetDisplay("Allow Friday", "Enable trading on Friday", "Schedule");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Type of candles used for analysis", "General");

_fibLevel = Param(nameof(FibLevel), 0.57m)
.SetDisplay("Fibonacci Level", "Retracement level used to position pending orders", "Trading")
.SetRange(0m, 1m);
}

/// <summary>
/// Volume traded on each entry signal.
/// </summary>
public decimal VolumePerTrade
{
get => _volume.Value;
set => _volume.Value = value;
}

/// <summary>
/// Take profit multiplier relative to the stop distance.
/// </summary>
public decimal TakeProfitMultiplier
{
get => _takeProfitMultiplier.Value;
set => _takeProfitMultiplier.Value = value;
}

/// <summary>
/// ZigZag depth parameter.
/// </summary>
public int ZigZagDepth
{
get => _zigZagDepth.Value;
set => _zigZagDepth.Value = value;
}

/// <summary>
/// ZigZag deviation parameter expressed in points.
/// </summary>
public decimal ZigZagDeviation
{
get => _zigZagDeviation.Value;
set => _zigZagDeviation.Value = value;
}

/// <summary>
/// ZigZag backstep parameter.
/// </summary>
public int ZigZagBackstep
{
get => _zigZagBackstep.Value;
set => _zigZagBackstep.Value = value;
}

/// <summary>
/// Number of candles to keep a pending order active.
/// </summary>
public int WaitBarsAfterSignal
{
get => _waitBars.Value;
set => _waitBars.Value = value;
}

/// <summary>
/// Period of the fast EMA filter.
/// </summary>
public int FastEmaPeriod
{
get => _fastEmaPeriod.Value;
set => _fastEmaPeriod.Value = value;
}

/// <summary>
/// Period of the slow EMA filter.
/// </summary>
public int SlowEmaPeriod
{
get => _slowEmaPeriod.Value;
set => _slowEmaPeriod.Value = value;
}

/// <summary>
/// Determines whether Monday is tradable.
/// </summary>
public bool AllowMonday
{
get => _allowMonday.Value;
set => _allowMonday.Value = value;
}

/// <summary>
/// Determines whether Tuesday is tradable.
/// </summary>
public bool AllowTuesday
{
get => _allowTuesday.Value;
set => _allowTuesday.Value = value;
}

/// <summary>
/// Determines whether Wednesday is tradable.
/// </summary>
public bool AllowWednesday
{
get => _allowWednesday.Value;
set => _allowWednesday.Value = value;
}

/// <summary>
/// Determines whether Thursday is tradable.
/// </summary>
public bool AllowThursday
{
get => _allowThursday.Value;
set => _allowThursday.Value = value;
}

/// <summary>
/// Determines whether Friday is tradable.
/// </summary>
public bool AllowFriday
{
get => _allowFriday.Value;
set => _allowFriday.Value = value;
}

/// <summary>
/// Candle type used for analysis.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Fibonacci retracement level used to place pending orders.
/// </summary>
public decimal FibLevel
{
get => _fibLevel.Value;
set => _fibLevel.Value = value;
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

_pivots.Clear();
_lastZigZagHigh = 0m;
_lastZigZagLow = 0m;

_previousFast = null;
_previousSlow = null;

_pendingOrder = null;
_plannedStop = null;
_plannedTake = null;
_plannedDirection = 0;
_barsSinceOrder = 0;

_activeStop = null;
_activeTake = null;
_activeDirection = 0;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

_zigZag = new ZigZagIndicator
{
Depth = ZigZagDepth,
Deviation = ZigZagDeviation,
BackStep = ZigZagBackstep
};

_fastEma = new EMA { Length = FastEmaPeriod };
_slowEma = new EMA { Length = SlowEmaPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(_zigZag, _fastEma, _slowEma, ProcessCandle)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawCandles(area, subscription);
DrawIndicator(area, _fastEma);
DrawIndicator(area, _slowEma);
DrawIndicator(area, _zigZag);
DrawOwnTrades(area);
}
}

/// <inheritdoc />
protected override void OnOrderChanged(Order order)
{
base.OnOrderChanged(order);

if (_pendingOrder != null && order == _pendingOrder)
{
switch (order.State)
{
case OrderStates.Done:
case OrderStates.DonePartially:
_activeStop = _plannedStop;
_activeTake = _plannedTake;
_activeDirection = _plannedDirection;
if (order.State == OrderStates.Done)
ResetPendingOrderState();
break;

case OrderStates.Failed:
case OrderStates.Cancelled:
ResetPendingOrderState();
break;
}
}
}

private void ProcessCandle(ICandleMessage candle, decimal zigZagValue, decimal fastEmaValue, decimal slowEmaValue)
{
if (candle.State != CandleStates.Finished)
return;

UpdateActiveProtection(candle);
UpdatePendingOrderLifetime();

if (!IsTradingDayAllowed(candle.OpenTime.DayOfWeek))
{
CancelPendingOrder();
_previousFast = fastEmaValue;
_previousSlow = slowEmaValue;
return;
}

var (newHigh, newLow) = UpdateZigZagState(candle, zigZagValue);

if (_pendingOrder == null && Position == 0m && (newHigh || newLow) &&
_previousFast.HasValue && _previousSlow.HasValue)
{
TryPlacePendingOrder(_previousFast.Value, _previousSlow.Value);
}

_previousFast = fastEmaValue;
_previousSlow = slowEmaValue;

if (Position == 0m && _activeDirection != 0)
ClearActiveProtection();
}

private void UpdatePendingOrderLifetime()
{
if (_pendingOrder == null)
return;

if (_pendingOrder.State != OrderStates.Active)
return;

if (WaitBarsAfterSignal <= 0)
return;

_barsSinceOrder++;
if (_barsSinceOrder >= WaitBarsAfterSignal)
CancelPendingOrder();
}

private void UpdateActiveProtection(ICandleMessage candle)
{
if (_activeDirection == 1 && Position > 0m && _activeStop.HasValue && _activeTake.HasValue)
{
if (candle.LowPrice <= _activeStop.Value || candle.HighPrice >= _activeTake.Value)
{
SellMarket(Position);
ClearActiveProtection();
}
}
else if (_activeDirection == -1 && Position < 0m && _activeStop.HasValue && _activeTake.HasValue)
{
if (candle.HighPrice >= _activeStop.Value || candle.LowPrice <= _activeTake.Value)
{
BuyMarket(-Position);
ClearActiveProtection();
}
}
}

private void TryPlacePendingOrder(decimal previousFast, decimal previousSlow)
{
if (!IsFormedAndOnlineAndAllowTrading())
return;

var high = _lastZigZagHigh;
var low = _lastZigZagLow;

if (high <= 0m || low <= 0m || high <= low)
return;

var volume = VolumePerTrade;
if (volume <= 0m)
return;

decimal fibPrice;
decimal stopPrice;
decimal takePrice;
int direction;

if (previousFast > previousSlow)
{
fibPrice = low + (high - low) * FibLevel;
stopPrice = low;
var slDistance = fibPrice - stopPrice;
if (slDistance <= 0m)
return;

takePrice = fibPrice + slDistance * TakeProfitMultiplier;
direction = 1;
}
else if (previousFast < previousSlow)
{
fibPrice = high - (high - low) * FibLevel;
stopPrice = high;
var slDistance = stopPrice - fibPrice;
if (slDistance <= 0m)
return;

takePrice = fibPrice - slDistance * TakeProfitMultiplier;
direction = -1;
}
else
{
return;
}

var minDistance = GetMinimumDistance();
if (minDistance > 0m)
{
if (direction == 1)
{
if (fibPrice - stopPrice < minDistance)
return;

if (takePrice - fibPrice < minDistance)
return;
}
else
{
if (stopPrice - fibPrice < minDistance)
return;

if (fibPrice - takePrice < minDistance)
return;
}
}

Order order;
if (direction == 1)
order = BuyLimit(volume, fibPrice);
else
order = SellLimit(volume, fibPrice);

if (order is null)
return;

_pendingOrder = order;
_plannedStop = stopPrice;
_plannedTake = takePrice;
_plannedDirection = direction;
_barsSinceOrder = 0;
}

private (bool newHigh, bool newLow) UpdateZigZagState(ICandleMessage candle, decimal zigZagValue)
{
var newHigh = false;
var newLow = false;

if (zigZagValue == 0m)
return (false, false);

var index = _pivots.FindIndex(p => p.Time == candle.OpenTime);
if (index >= 0)
{
if (_pivots[index].Price == zigZagValue)
return (false, false);

_pivots[index] = (candle.OpenTime, zigZagValue);
}
else
{
_pivots.Add((candle.OpenTime, zigZagValue));
if (_pivots.Count > 300)
_pivots.RemoveAt(0);
}

if (_pivots.Count < 2)
return (false, false);

var previous = _pivots[^2];
var last = _pivots[^1];
var isHigh = last.Price > previous.Price;

if (isHigh)
{
if (_lastZigZagHigh != last.Price)
{
_lastZigZagHigh = last.Price;
newHigh = true;
}

if (_lastZigZagLow != previous.Price)
{
_lastZigZagLow = previous.Price;
newLow = true;
}
}
else
{
if (_lastZigZagLow != last.Price)
{
_lastZigZagLow = last.Price;
newLow = true;
}

if (_lastZigZagHigh != previous.Price)
{
_lastZigZagHigh = previous.Price;
newHigh = true;
}
}

return (newHigh, newLow);
}

private bool IsTradingDayAllowed(DayOfWeek day)
{
return day switch
{
DayOfWeek.Monday => AllowMonday,
DayOfWeek.Tuesday => AllowTuesday,
DayOfWeek.Wednesday => AllowWednesday,
DayOfWeek.Thursday => AllowThursday,
DayOfWeek.Friday => AllowFriday,
_ => false,
};
}

private decimal GetMinimumDistance()
{
if (Security?.PriceStep is { } step && step > 0m)
return step;

return 0m;
}

private void CancelPendingOrder()
{
if (_pendingOrder == null)
return;

if (_pendingOrder.State == OrderStates.Active)
CancelOrder(_pendingOrder);
else if (_pendingOrder.State == OrderStates.None)
ResetPendingOrderState();
}

private void ResetPendingOrderState()
{
_pendingOrder = null;
_plannedStop = null;
_plannedTake = null;
_plannedDirection = 0;
_barsSinceOrder = 0;
}

private void ClearActiveProtection()
{
_activeStop = null;
_activeTake = null;
_activeDirection = 0;
}
}
