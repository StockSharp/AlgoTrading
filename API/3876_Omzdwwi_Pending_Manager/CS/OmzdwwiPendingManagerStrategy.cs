using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that emulates the OMZDWWI pending order manager expert.
/// </summary>
public class OmzdwwiPendingManagerStrategy : Strategy
{
private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<bool> _waitClose;
private readonly StrategyParam<bool> _enableBuyStop;
private readonly StrategyParam<bool> _enableSellLimit;
private readonly StrategyParam<bool> _enableSellStop;
private readonly StrategyParam<bool> _enableBuyLimit;

private readonly StrategyParam<int> _maxMarketOrders;
private readonly StrategyParam<decimal> _marketTakeProfitPoints;
private readonly StrategyParam<decimal> _marketStopLossPoints;
private readonly StrategyParam<decimal> _marketTrailingOffsetPoints;
private readonly StrategyParam<decimal> _marketTrailingStepPoints;
private readonly StrategyParam<bool> _requireProfitBeforeTrailing;

private readonly StrategyParam<decimal> _stopStepPoints;
private readonly StrategyParam<decimal> _stopTakeProfitPoints;
private readonly StrategyParam<decimal> _stopStopLossPoints;
private readonly StrategyParam<decimal> _stopTrailingOffsetPoints;
private readonly StrategyParam<decimal> _stopTrailingStepPoints;

private readonly StrategyParam<decimal> _limitStepPoints;
private readonly StrategyParam<decimal> _limitTakeProfitPoints;
private readonly StrategyParam<decimal> _limitStopLossPoints;
private readonly StrategyParam<decimal> _limitTrailingOffsetPoints;
private readonly StrategyParam<decimal> _limitTrailingStepPoints;

private readonly StrategyParam<bool> _useTimeSignals;
private readonly StrategyParam<int> _signalHour;
private readonly StrategyParam<int> _signalMinute;
private readonly StrategyParam<bool> _timeBuySignal;
private readonly StrategyParam<bool> _timeSellSignal;
private readonly StrategyParam<bool> _timeBuyStopSignal;
private readonly StrategyParam<bool> _timeSellLimitSignal;
private readonly StrategyParam<bool> _timeSellStopSignal;
private readonly StrategyParam<bool> _timeBuyLimitSignal;

private readonly StrategyParam<decimal> _exitProfitPoints;
private readonly StrategyParam<decimal> _slippagePoints;

private readonly StrategyParam<bool> _useGlobalLevels;
private readonly StrategyParam<decimal> _globalTakeProfitPercent;
private readonly StrategyParam<decimal> _globalStopLossPercent;

private decimal _priceStep;
private decimal? _lastBid;
private decimal? _lastAsk;
private DateTime? _lastTriggeredMinute;
private decimal _initialBalance;
private bool _globalTakeTriggered;
private bool _globalStopTriggered;

private bool _pendingTimeBuy;
private bool _pendingTimeSell;
private bool _pendingTimeBuyStop;
private bool _pendingTimeSellLimit;
private bool _pendingTimeSellStop;
private bool _pendingTimeBuyLimit;

private Order? _buyStopOrder;
private Order? _sellStopOrder;
private Order? _buyLimitOrder;
private Order? _sellLimitOrder;

private decimal? _longStopPrice;
private decimal? _longTakeProfitPrice;
private decimal? _shortStopPrice;
private decimal? _shortTakeProfitPrice;

/// <summary>
/// Initializes a new instance of the <see cref="OmzdwwiPendingManagerStrategy"/> class.
/// </summary>
public OmzdwwiPendingManagerStrategy()
{
_orderVolume = Param(nameof(OrderVolume), 0.2m)
.SetGreaterThanZero()
.SetDisplay("Order volume", "Trade volume used for market and pending orders", "General")
.SetCanOptimize(true);

_waitClose = Param(nameof(WaitClose), true)
.SetDisplay("Wait for flat position", "Disallow new entries until all positions are closed", "Execution");

_enableBuyStop = Param(nameof(EnableBuyStop), true)
.SetDisplay("Enable buy stop", "Maintain a buy stop pending order", "Pending Orders");

_enableSellLimit = Param(nameof(EnableSellLimit), false)
.SetDisplay("Enable sell limit", "Maintain a sell limit pending order", "Pending Orders");

_enableSellStop = Param(nameof(EnableSellStop), true)
.SetDisplay("Enable sell stop", "Maintain a sell stop pending order", "Pending Orders");

_enableBuyLimit = Param(nameof(EnableBuyLimit), false)
.SetDisplay("Enable buy limit", "Maintain a buy limit pending order", "Pending Orders");

_maxMarketOrders = Param(nameof(MaxMarketOrders), 2)
.SetNotNegative()
.SetDisplay("Maximum market entries", "Maximum number of concurrent market lots per direction", "Execution")
.SetCanOptimize(true);

_marketTakeProfitPoints = Param(nameof(MarketTakeProfitPoints), 200m)
.SetNotNegative()
.SetDisplay("Market take profit", "Take profit distance for market positions in points", "Risk Management")
.SetCanOptimize(true);

_marketStopLossPoints = Param(nameof(MarketStopLossPoints), 100m)
.SetNotNegative()
.SetDisplay("Market stop loss", "Stop loss distance for market positions in points", "Risk Management")
.SetCanOptimize(true);

_marketTrailingOffsetPoints = Param(nameof(MarketTrailingOffsetPoints), 100m)
.SetNotNegative()
.SetDisplay("Market trailing offset", "Offset in points used for trailing market positions", "Risk Management")
.SetCanOptimize(true);

_marketTrailingStepPoints = Param(nameof(MarketTrailingStepPoints), 10m)
.SetNotNegative()
.SetDisplay("Market trailing step", "Minimal improvement in points before moving the trailing stop", "Risk Management")
.SetCanOptimize(true);

_requireProfitBeforeTrailing = Param(nameof(RequireProfitBeforeTrailing), true)
.SetDisplay("Require profit for trailing", "Start trailing only after price moved by the trailing offset", "Risk Management");

_stopStepPoints = Param(nameof(StopStepPoints), 50m)
.SetGreaterThanZero()
.SetDisplay("Stop order distance", "Distance in points between price and stop orders", "Pending Orders")
.SetCanOptimize(true);

_stopTakeProfitPoints = Param(nameof(StopTakeProfitPoints), 200m)
.SetNotNegative()
.SetDisplay("Stop order take profit", "Take profit distance for triggered stop orders in points", "Pending Orders")
.SetCanOptimize(true);

_stopStopLossPoints = Param(nameof(StopStopLossPoints), 100m)
.SetNotNegative()
.SetDisplay("Stop order stop loss", "Stop loss distance for triggered stop orders in points", "Pending Orders")
.SetCanOptimize(true);

_stopTrailingOffsetPoints = Param(nameof(StopTrailingOffsetPoints), 0m)
.SetNotNegative()
.SetDisplay("Stop order trailing offset", "Offset in points used when trailing stop orders", "Pending Orders")
.SetCanOptimize(true);

_stopTrailingStepPoints = Param(nameof(StopTrailingStepPoints), 3m)
.SetNotNegative()
.SetDisplay("Stop order trailing step", "Minimal movement in points required before trailing stop orders", "Pending Orders")
.SetCanOptimize(true);

_limitStepPoints = Param(nameof(LimitStepPoints), 50m)
.SetGreaterThanZero()
.SetDisplay("Limit order distance", "Distance in points between price and limit orders", "Pending Orders")
.SetCanOptimize(true);

_limitTakeProfitPoints = Param(nameof(LimitTakeProfitPoints), 200m)
.SetNotNegative()
.SetDisplay("Limit order take profit", "Take profit distance for triggered limit orders in points", "Pending Orders")
.SetCanOptimize(true);

_limitStopLossPoints = Param(nameof(LimitStopLossPoints), 100m)
.SetNotNegative()
.SetDisplay("Limit order stop loss", "Stop loss distance for triggered limit orders in points", "Pending Orders")
.SetCanOptimize(true);

_limitTrailingOffsetPoints = Param(nameof(LimitTrailingOffsetPoints), 0m)
.SetNotNegative()
.SetDisplay("Limit order trailing offset", "Offset in points used when trailing limit orders", "Pending Orders")
.SetCanOptimize(true);

_limitTrailingStepPoints = Param(nameof(LimitTrailingStepPoints), 3m)
.SetNotNegative()
.SetDisplay("Limit order trailing step", "Minimal movement in points required before trailing limit orders", "Pending Orders")
.SetCanOptimize(true);

_useTimeSignals = Param(nameof(UseTimeSignals), true)
.SetDisplay("Use time signals", "Trigger entries at the configured hour and minute", "Time Management");

_signalHour = Param(nameof(SignalHour), 23)
.SetRange(0, 23)
.SetDisplay("Signal hour", "Hour when time based signals are generated", "Time Management");

_signalMinute = Param(nameof(SignalMinute), 59)
.SetRange(0, 59)
.SetDisplay("Signal minute", "Minute when time based signals are generated", "Time Management");

_timeBuySignal = Param(nameof(TimeBuySignal), false)
.SetDisplay("Trigger buy", "Activate buy market order on scheduled time", "Time Management");

_timeSellSignal = Param(nameof(TimeSellSignal), false)
.SetDisplay("Trigger sell", "Activate sell market order on scheduled time", "Time Management");

_timeBuyStopSignal = Param(nameof(TimeBuyStopSignal), true)
.SetDisplay("Trigger buy stop", "Activate buy stop order on scheduled time", "Time Management");

_timeSellLimitSignal = Param(nameof(TimeSellLimitSignal), false)
.SetDisplay("Trigger sell limit", "Activate sell limit order on scheduled time", "Time Management");

_timeSellStopSignal = Param(nameof(TimeSellStopSignal), true)
.SetDisplay("Trigger sell stop", "Activate sell stop order on scheduled time", "Time Management");

_timeBuyLimitSignal = Param(nameof(TimeBuyLimitSignal), false)
.SetDisplay("Trigger buy limit", "Activate buy limit order on scheduled time", "Time Management");

_exitProfitPoints = Param(nameof(ExitProfitPoints), 0m)
.SetNotNegative()
.SetDisplay("Pips profit", "Close positions once specified profit in points is reached", "Risk Management")
.SetCanOptimize(true);

_slippagePoints = Param(nameof(SlippagePoints), 3m)
.SetNotNegative()
.SetDisplay("Slippage reserve", "Reserved parameter for compatibility with original expert", "Execution")
.SetCanOptimize(true);

_useGlobalLevels = Param(nameof(UseGlobalLevels), true)
.SetDisplay("Use global levels", "Monitor account level profit and loss thresholds", "Account Monitoring");

_globalTakeProfitPercent = Param(nameof(GlobalTakeProfitPercent), 2m)
.SetNotNegative()
.SetDisplay("Global take profit", "Equity gain percentage that triggers an informational alert", "Account Monitoring")
.SetCanOptimize(true);

_globalStopLossPercent = Param(nameof(GlobalStopLossPercent), 2m)
.SetNotNegative()
.SetDisplay("Global stop loss", "Equity drawdown percentage that triggers an informational alert", "Account Monitoring")
.SetCanOptimize(true);
}

/// <summary>
/// Trade volume used for all requests.
/// </summary>
public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
}

/// <summary>
/// Whether new entries are postponed until all positions are closed.
/// </summary>
public bool WaitClose
{
get => _waitClose.Value;
set => _waitClose.Value = value;
}

/// <summary>
/// Whether to keep a buy stop pending order.
/// </summary>
public bool EnableBuyStop
{
get => _enableBuyStop.Value;
set => _enableBuyStop.Value = value;
}

/// <summary>
/// Whether to keep a sell limit pending order.
/// </summary>
public bool EnableSellLimit
{
get => _enableSellLimit.Value;
set => _enableSellLimit.Value = value;
}

/// <summary>
/// Whether to keep a sell stop pending order.
/// </summary>
public bool EnableSellStop
{
get => _enableSellStop.Value;
set => _enableSellStop.Value = value;
}

/// <summary>
/// Whether to keep a buy limit pending order.
/// </summary>
public bool EnableBuyLimit
{
get => _enableBuyLimit.Value;
set => _enableBuyLimit.Value = value;
}

/// <summary>
/// Maximum number of concurrent market orders per direction.
/// </summary>
public int MaxMarketOrders
{
get => _maxMarketOrders.Value;
set => _maxMarketOrders.Value = value;
}

/// <summary>
/// Take profit distance for market positions in points.
/// </summary>
public decimal MarketTakeProfitPoints
{
get => _marketTakeProfitPoints.Value;
set => _marketTakeProfitPoints.Value = value;
}

/// <summary>
/// Stop loss distance for market positions in points.
/// </summary>
public decimal MarketStopLossPoints
{
get => _marketStopLossPoints.Value;
set => _marketStopLossPoints.Value = value;
}

/// <summary>
/// Trailing offset in points for market positions.
/// </summary>
public decimal MarketTrailingOffsetPoints
{
get => _marketTrailingOffsetPoints.Value;
set => _marketTrailingOffsetPoints.Value = value;
}

/// <summary>
/// Trailing step in points for market positions.
/// </summary>
public decimal MarketTrailingStepPoints
{
get => _marketTrailingStepPoints.Value;
set => _marketTrailingStepPoints.Value = value;
}

/// <summary>
/// Require profit before activating trailing logic.
/// </summary>
public bool RequireProfitBeforeTrailing
{
get => _requireProfitBeforeTrailing.Value;
set => _requireProfitBeforeTrailing.Value = value;
}

/// <summary>
/// Distance in points between price and stop orders.
/// </summary>
public decimal StopStepPoints
{
get => _stopStepPoints.Value;
set => _stopStepPoints.Value = value;
}

/// <summary>
/// Take profit in points for stop orders.
/// </summary>
public decimal StopTakeProfitPoints
{
get => _stopTakeProfitPoints.Value;
set => _stopTakeProfitPoints.Value = value;
}

/// <summary>
/// Stop loss in points for stop orders.
/// </summary>
public decimal StopStopLossPoints
{
get => _stopStopLossPoints.Value;
set => _stopStopLossPoints.Value = value;
}

/// <summary>
/// Trailing offset in points for stop orders.
/// </summary>
public decimal StopTrailingOffsetPoints
{
get => _stopTrailingOffsetPoints.Value;
set => _stopTrailingOffsetPoints.Value = value;
}

/// <summary>
/// Trailing step in points for stop orders.
/// </summary>
public decimal StopTrailingStepPoints
{
get => _stopTrailingStepPoints.Value;
set => _stopTrailingStepPoints.Value = value;
}

/// <summary>
/// Distance in points between price and limit orders.
/// </summary>
public decimal LimitStepPoints
{
get => _limitStepPoints.Value;
set => _limitStepPoints.Value = value;
}

/// <summary>
/// Take profit in points for limit orders.
/// </summary>
public decimal LimitTakeProfitPoints
{
get => _limitTakeProfitPoints.Value;
set => _limitTakeProfitPoints.Value = value;
}

/// <summary>
/// Stop loss in points for limit orders.
/// </summary>
public decimal LimitStopLossPoints
{
get => _limitStopLossPoints.Value;
set => _limitStopLossPoints.Value = value;
}

/// <summary>
/// Trailing offset in points for limit orders.
/// </summary>
public decimal LimitTrailingOffsetPoints
{
get => _limitTrailingOffsetPoints.Value;
set => _limitTrailingOffsetPoints.Value = value;
}

/// <summary>
/// Trailing step in points for limit orders.
/// </summary>
public decimal LimitTrailingStepPoints
{
get => _limitTrailingStepPoints.Value;
set => _limitTrailingStepPoints.Value = value;
}

/// <summary>
/// Use time based signals.
/// </summary>
public bool UseTimeSignals
{
get => _useTimeSignals.Value;
set => _useTimeSignals.Value = value;
}

/// <summary>
/// Hour component of the time signal.
/// </summary>
public int SignalHour
{
get => _signalHour.Value;
set => _signalHour.Value = value;
}

/// <summary>
/// Minute component of the time signal.
/// </summary>
public int SignalMinute
{
get => _signalMinute.Value;
set => _signalMinute.Value = value;
}

/// <summary>
/// Whether to trigger a buy market order at the scheduled time.
/// </summary>
public bool TimeBuySignal
{
get => _timeBuySignal.Value;
set => _timeBuySignal.Value = value;
}

/// <summary>
/// Whether to trigger a sell market order at the scheduled time.
/// </summary>
public bool TimeSellSignal
{
get => _timeSellSignal.Value;
set => _timeSellSignal.Value = value;
}

/// <summary>
/// Whether to trigger a buy stop order at the scheduled time.
/// </summary>
public bool TimeBuyStopSignal
{
get => _timeBuyStopSignal.Value;
set => _timeBuyStopSignal.Value = value;
}

/// <summary>
/// Whether to trigger a sell limit order at the scheduled time.
/// </summary>
public bool TimeSellLimitSignal
{
get => _timeSellLimitSignal.Value;
set => _timeSellLimitSignal.Value = value;
}

/// <summary>
/// Whether to trigger a sell stop order at the scheduled time.
/// </summary>
public bool TimeSellStopSignal
{
get => _timeSellStopSignal.Value;
set => _timeSellStopSignal.Value = value;
}

/// <summary>
/// Whether to trigger a buy limit order at the scheduled time.
/// </summary>
public bool TimeBuyLimitSignal
{
get => _timeBuyLimitSignal.Value;
set => _timeBuyLimitSignal.Value = value;
}

/// <summary>
/// Close position once this additional profit target is reached.
/// </summary>
public decimal ExitProfitPoints
{
get => _exitProfitPoints.Value;
set => _exitProfitPoints.Value = value;
}

/// <summary>
/// Reserved parameter for execution slippage.
/// </summary>
public decimal SlippagePoints
{
get => _slippagePoints.Value;
set => _slippagePoints.Value = value;
}

/// <summary>
/// Use global account level profit and loss monitoring.
/// </summary>
public bool UseGlobalLevels
{
get => _useGlobalLevels.Value;
set => _useGlobalLevels.Value = value;
}

/// <summary>
/// Equity gain percentage that triggers an informational alert.
/// </summary>
public decimal GlobalTakeProfitPercent
{
get => _globalTakeProfitPercent.Value;
set => _globalTakeProfitPercent.Value = value;
}

/// <summary>
/// Equity drawdown percentage that triggers an informational alert.
/// </summary>
public decimal GlobalStopLossPercent
{
get => _globalStopLossPercent.Value;
set => _globalStopLossPercent.Value = value;
}

/// <inheritdoc />
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
return
[
(Security, DataType.Level1)
];
}

/// <inheritdoc />
protected override void OnReseted()
{
base.OnReseted();

_lastBid = null;
_lastAsk = null;
_lastTriggeredMinute = null;
_globalTakeTriggered = false;
_globalStopTriggered = false;
_pendingTimeBuy = false;
_pendingTimeSell = false;
_pendingTimeBuyStop = false;
_pendingTimeSellLimit = false;
_pendingTimeSellStop = false;
_pendingTimeBuyLimit = false;
_buyStopOrder = null;
_sellStopOrder = null;
_buyLimitOrder = null;
_sellLimitOrder = null;
_longStopPrice = null;
_longTakeProfitPrice = null;
_shortStopPrice = null;
_shortTakeProfitPrice = null;
_priceStep = 0m;
_initialBalance = 0m;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

if (Security == null)
throw new InvalidOperationException("Security is not specified.");

if (Portfolio == null)
throw new InvalidOperationException("Portfolio is not specified.");

if (OrderVolume <= 0m)
throw new InvalidOperationException("Order volume must be positive.");

_priceStep = GetPriceStep();
if (_priceStep <= 0m)
throw new InvalidOperationException("Security price step must be defined and positive.");

Volume = OrderVolume;

_initialBalance = Portfolio.CurrentValue ?? Portfolio.BeginValue ?? 0m;

SubscribeLevel1()
.Bind(ProcessLevel1)
.Start();

var area = CreateChartArea();
if (area != null)
{
DrawOwnTrades(area);
}
}

/// <inheritdoc />
protected override void OnOrderChanged(Order order)
{
base.OnOrderChanged(order);

if (order.Security != Security)
return;

if (order.State is OrderStates.Done or OrderStates.Canceled or OrderStates.Failed)
{
if (order == _buyStopOrder)
_buyStopOrder = null;
else if (order == _sellStopOrder)
_sellStopOrder = null;
else if (order == _buyLimitOrder)
_buyLimitOrder = null;
else if (order == _sellLimitOrder)
_sellLimitOrder = null;
}
}

/// <inheritdoc />
protected override void OnOrderRegisterFailed(Order order)
{
base.OnOrderRegisterFailed(order);

if (order.Security != Security)
return;

if (order == _buyStopOrder)
_buyStopOrder = null;
else if (order == _sellStopOrder)
_sellStopOrder = null;
else if (order == _buyLimitOrder)
_buyLimitOrder = null;
else if (order == _sellLimitOrder)
_sellLimitOrder = null;
}

/// <inheritdoc />
protected override void OnPositionChanged(decimal delta)
{
base.OnPositionChanged(delta);

if (Security == null)
return;

if (Position == 0m)
{
_longStopPrice = null;
_longTakeProfitPrice = null;
_shortStopPrice = null;
_shortTakeProfitPrice = null;
return;
}

var entryPrice = PositionPrice;
if (entryPrice <= 0m)
return;

if (Position > 0m)
{
_longStopPrice = MarketStopLossPoints > 0m ? entryPrice - MarketStopLossPoints * _priceStep : null;
_longTakeProfitPrice = MarketTakeProfitPoints > 0m ? entryPrice + MarketTakeProfitPoints * _priceStep : null;
_shortStopPrice = null;
_shortTakeProfitPrice = null;
}
else if (Position < 0m)
{
_shortStopPrice = MarketStopLossPoints > 0m ? entryPrice + MarketStopLossPoints * _priceStep : null;
_shortTakeProfitPrice = MarketTakeProfitPoints > 0m ? entryPrice - MarketTakeProfitPoints * _priceStep : null;
_longStopPrice = null;
_longTakeProfitPrice = null;
}
}

private void ProcessLevel1(Level1ChangeMessage message)
{
if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidObj) && bidObj != null)
_lastBid = (decimal)bidObj;

if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askObj) && askObj != null)
_lastAsk = (decimal)askObj;

var serverTime = message.ServerTime;
ProcessTimeSignals(serverTime);
ProcessGlobalLevels();
PlaceMarketOrders();
PlacePendingOrders();
UpdateMarketTrailing();
UpdatePendingTrailing();
CheckMarketExits();
}

private void ProcessTimeSignals(DateTimeOffset serverTime)
{
if (!UseTimeSignals)
return;

var currentMinute = new DateTime(serverTime.Year, serverTime.Month, serverTime.Day, serverTime.Hour, serverTime.Minute, 0, serverTime.Offset);
if (_lastTriggeredMinute != null && _lastTriggeredMinute == currentMinute)
return;

if (serverTime.Hour == SignalHour && serverTime.Minute == SignalMinute)
{
_pendingTimeBuy = TimeBuySignal;
_pendingTimeSell = TimeSellSignal;
_pendingTimeBuyStop = TimeBuyStopSignal;
_pendingTimeSellLimit = TimeSellLimitSignal;
_pendingTimeSellStop = TimeSellStopSignal;
_pendingTimeBuyLimit = TimeBuyLimitSignal;
_lastTriggeredMinute = currentMinute;
}
}

private void ProcessGlobalLevels()
{
if (!UseGlobalLevels)
return;

var portfolio = Portfolio;
if (portfolio == null)
return;

var balance = portfolio.BeginValue ?? _initialBalance;
if (balance <= 0m)
balance = _initialBalance;

var equity = portfolio.CurrentValue ?? balance;
if (balance <= 0m)
return;

var profitPercent = (equity - balance) / balance * 100m;

if (!_globalTakeTriggered && GlobalTakeProfitPercent > 0m && profitPercent >= GlobalTakeProfitPercent)
{
LogInfo($"Global take profit reached: {profitPercent:F2}% (threshold {GlobalTakeProfitPercent:F2}%).");
_globalTakeTriggered = true;
_globalStopTriggered = false;
}
else if (!_globalStopTriggered && GlobalStopLossPercent > 0m && -profitPercent >= GlobalStopLossPercent)
{
LogWarning($"Global stop loss reached: {profitPercent:F2}% (threshold -{GlobalStopLossPercent:F2}%).");
_globalStopTriggered = true;
_globalTakeTriggered = false;
}
}

private void PlaceMarketOrders()
{
if (!IsFormedAndOnlineAndAllowTrading())
return;

if (OrderVolume <= 0m)
return;

if (_pendingTimeBuy && CanOpenPosition(Sides.Buy))
{
BuyMarket(OrderVolume);
_pendingTimeBuy = false;
}

if (_pendingTimeSell && CanOpenPosition(Sides.Sell))
{
SellMarket(OrderVolume);
_pendingTimeSell = false;
}
}

private bool CanOpenPosition(Sides side)
{
if (OrderVolume <= 0m)
return false;

if (WaitClose)
{
if (side == Sides.Buy)
return Position <= 0m;

return Position >= 0m;
}

if (MaxMarketOrders <= 0)
return true;

var lots = OrderVolume > 0m ? Math.Abs(Position) / OrderVolume : 0m;
return lots + 1m <= MaxMarketOrders + 1e-6m;
}

private void PlacePendingOrders()
{
if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_lastAsk == null || _lastBid == null)
return;

if (WaitClose && Position != 0m)
return;

var ask = _lastAsk.Value;
var bid = _lastBid.Value;

if ((EnableSellStop || _pendingTimeSellStop) && StopStepPoints > 0m)
{
TryPlaceStopOrder(ref _sellStopOrder, Sides.Sell, bid - StopStepPoints * _priceStep);
if (_sellStopOrder != null && _sellStopOrder.State.IsActive())
_pendingTimeSellStop = false;
}

if ((EnableBuyStop || _pendingTimeBuyStop) && StopStepPoints > 0m)
{
TryPlaceStopOrder(ref _buyStopOrder, Sides.Buy, ask + StopStepPoints * _priceStep);
if (_buyStopOrder != null && _buyStopOrder.State.IsActive())
_pendingTimeBuyStop = false;
}

if ((EnableBuyLimit || _pendingTimeBuyLimit) && LimitStepPoints > 0m)
{
TryPlaceLimitOrder(ref _buyLimitOrder, Sides.Buy, ask - LimitStepPoints * _priceStep);
if (_buyLimitOrder != null && _buyLimitOrder.State.IsActive())
_pendingTimeBuyLimit = false;
}

if ((EnableSellLimit || _pendingTimeSellLimit) && LimitStepPoints > 0m)
{
TryPlaceLimitOrder(ref _sellLimitOrder, Sides.Sell, bid + LimitStepPoints * _priceStep);
if (_sellLimitOrder != null && _sellLimitOrder.State.IsActive())
_pendingTimeSellLimit = false;
}
}

private void TryPlaceStopOrder(ref Order? orderField, Sides side, decimal targetPrice)
{
var price = NormalizePrice(targetPrice);
if (price <= 0m)
return;

if (orderField == null)
{
orderField = side == Sides.Buy ? BuyStop(price) : SellStop(price);
return;
}

if (orderField.State.IsActive() && Math.Abs((orderField.Price ?? 0m) - price) >= _priceStep / 2m)
{
ReRegisterOrder(orderField, price, orderField.Volume ?? OrderVolume);
}
}

private void TryPlaceLimitOrder(ref Order? orderField, Sides side, decimal targetPrice)
{
var price = NormalizePrice(targetPrice);
if (price <= 0m)
return;

if (orderField == null)
{
orderField = side == Sides.Buy ? BuyLimit(price) : SellLimit(price);
return;
}

if (orderField.State.IsActive() && Math.Abs((orderField.Price ?? 0m) - price) >= _priceStep / 2m)
{
ReRegisterOrder(orderField, price, orderField.Volume ?? OrderVolume);
}
}

private void UpdateMarketTrailing()
{
if (!IsFormedAndOnline())
return;

if (_lastAsk == null || _lastBid == null)
return;

if (Position > 0m)
{
var entryPrice = PositionPrice;
if (entryPrice <= 0m)
return;

var desiredStop = _lastBid.Value - MarketTrailingOffsetPoints * _priceStep;
if (MarketTrailingOffsetPoints > 0m && (!RequireProfitBeforeTrailing || _lastBid.Value - entryPrice >= MarketTrailingOffsetPoints * _priceStep))
{
if (_longStopPrice == null || desiredStop - _longStopPrice.Value >= MarketTrailingStepPoints * _priceStep)
{
_longStopPrice = desiredStop;
}
}
}
else if (Position < 0m)
{
var entryPrice = PositionPrice;
if (entryPrice <= 0m)
return;

var desiredStop = _lastAsk.Value + MarketTrailingOffsetPoints * _priceStep;
if (MarketTrailingOffsetPoints > 0m && (!RequireProfitBeforeTrailing || entryPrice - _lastAsk.Value >= MarketTrailingOffsetPoints * _priceStep))
{
if (_shortStopPrice == null || _shortStopPrice.Value - desiredStop >= MarketTrailingStepPoints * _priceStep)
{
_shortStopPrice = desiredStop;
}
}
}
}

private void UpdatePendingTrailing()
{
if (!IsFormedAndOnline())
return;

if (_lastAsk == null || _lastBid == null)
return;

var ask = _lastAsk.Value;
var bid = _lastBid.Value;

if (_buyStopOrder != null && _buyStopOrder.State.IsActive() && StopTrailingOffsetPoints > 0m)
{
var threshold = (_buyStopOrder.Price ?? 0m) - (StopTrailingOffsetPoints + StopTrailingStepPoints) * _priceStep;
if (ask < threshold)
{
var newPrice = NormalizePrice(ask + StopTrailingOffsetPoints * _priceStep);
ReRegisterOrder(_buyStopOrder, newPrice, _buyStopOrder.Volume ?? OrderVolume);
}
}

if (_sellStopOrder != null && _sellStopOrder.State.IsActive() && StopTrailingOffsetPoints > 0m)
{
var threshold = (_sellStopOrder.Price ?? 0m) + (StopTrailingOffsetPoints + StopTrailingStepPoints) * _priceStep;
if (bid > threshold)
{
var newPrice = NormalizePrice(bid - StopTrailingOffsetPoints * _priceStep);
ReRegisterOrder(_sellStopOrder, newPrice, _sellStopOrder.Volume ?? OrderVolume);
}
}

if (_buyLimitOrder != null && _buyLimitOrder.State.IsActive() && LimitTrailingOffsetPoints > 0m)
{
var threshold = (_buyLimitOrder.Price ?? 0m) + (LimitTrailingOffsetPoints + LimitTrailingStepPoints) * _priceStep;
if (ask > threshold)
{
var newPrice = NormalizePrice(ask - LimitTrailingOffsetPoints * _priceStep);
ReRegisterOrder(_buyLimitOrder, newPrice, _buyLimitOrder.Volume ?? OrderVolume);
}
}

if (_sellLimitOrder != null && _sellLimitOrder.State.IsActive() && LimitTrailingOffsetPoints > 0m)
{
var threshold = (_sellLimitOrder.Price ?? 0m) - (LimitTrailingOffsetPoints + LimitTrailingStepPoints) * _priceStep;
if (bid < threshold)
{
var newPrice = NormalizePrice(bid + LimitTrailingOffsetPoints * _priceStep);
ReRegisterOrder(_sellLimitOrder, newPrice, _sellLimitOrder.Volume ?? OrderVolume);
}
}
}

private void CheckMarketExits()
{
if (!IsFormedAndOnlineAndAllowTrading())
return;

if (_lastAsk == null || _lastBid == null)
return;

if (Position > 0m)
{
var exitVolume = Math.Abs(Position);
var bid = _lastBid.Value;
var entryPrice = PositionPrice;

if (_longStopPrice != null && bid <= _longStopPrice.Value)
{
SellMarket(exitVolume);
_longStopPrice = null;
_longTakeProfitPrice = null;
return;
}

if (_longTakeProfitPrice != null && bid >= _longTakeProfitPrice.Value)
{
SellMarket(exitVolume);
_longStopPrice = null;
_longTakeProfitPrice = null;
return;
}

if (ExitProfitPoints > 0m && entryPrice > 0m)
{
var target = entryPrice + ExitProfitPoints * _priceStep;
if (bid >= target)
{
SellMarket(exitVolume);
_longStopPrice = null;
_longTakeProfitPrice = null;
}
}
}
else if (Position < 0m)
{
var exitVolume = Math.Abs(Position);
var ask = _lastAsk.Value;
var entryPrice = PositionPrice;

if (_shortStopPrice != null && ask >= _shortStopPrice.Value)
{
BuyMarket(exitVolume);
_shortStopPrice = null;
_shortTakeProfitPrice = null;
return;
}

if (_shortTakeProfitPrice != null && ask <= _shortTakeProfitPrice.Value)
{
BuyMarket(exitVolume);
_shortStopPrice = null;
_shortTakeProfitPrice = null;
return;
}

if (ExitProfitPoints > 0m && entryPrice > 0m)
{
var target = entryPrice - ExitProfitPoints * _priceStep;
if (ask <= target)
{
BuyMarket(exitVolume);
_shortStopPrice = null;
_shortTakeProfitPrice = null;
}
}
}
}

private decimal GetPriceStep()
{
var security = Security;
if (security == null)
return 0m;

var step = security.PriceStep ?? 0m;
if (step > 0m)
return step;

if (security.MinStep != null && security.MinStep > 0m)
return security.MinStep.Value;

if (security.Decimals != null && security.Decimals.Value > 0)
return (decimal)Math.Pow(10, -security.Decimals.Value);

return 0m;
}

private decimal NormalizePrice(decimal price)
{
var security = Security;
return security?.ShrinkPrice(price) ?? price;
}
}
