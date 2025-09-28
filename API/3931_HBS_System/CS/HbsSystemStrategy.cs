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
/// Pending stop breakout strategy converted from the MetaTrader "HBS system" expert advisor.
/// </summary>
public class HbsSystemStrategy : Strategy
{
private sealed class PendingOrderInfo
{
public required Sides Side { get; init; }
public required decimal EntryPrice { get; init; }
public required decimal StopPrice { get; init; }
public required decimal TakeProfitPrice { get; init; }
public required decimal Volume { get; init; }
}

private readonly StrategyParam<int> _maPeriod;
private readonly StrategyParam<int> _stopLossBuyPoints;
private readonly StrategyParam<int> _trailingStopBuyPoints;
private readonly StrategyParam<int> _stopLossSellPoints;
private readonly StrategyParam<int> _trailingStopSellPoints;
private readonly StrategyParam<decimal> _orderVolume;
private readonly StrategyParam<int> _entryOffsetPoints;
private readonly StrategyParam<int> _secondaryTakeProfitPoints;
private readonly StrategyParam<decimal> _roundingFactor;
private readonly StrategyParam<DataType> _candleType;

private ExponentialMovingAverage _ema;
private decimal? _previousOpen;
private decimal? _previousClose;
private decimal? _previousEma;
private decimal? _lastBuyLevel;
private decimal? _lastSellLevel;

private readonly Dictionary<Order, PendingOrderInfo> _pendingOrders = new();
private readonly HashSet<Order> _takeProfitOrders = new();

private Order _longStopOrder;
private Order _shortStopOrder;
private decimal? _longStopPrice;
private decimal? _shortStopPrice;

private decimal _pointValue;

/// <summary>
/// Initializes a new instance of <see cref="HbsSystemStrategy"/>.
/// </summary>
public HbsSystemStrategy()
{
_maPeriod = Param(nameof(MaPeriod), 200)
.SetGreaterThanZero()
.SetDisplay("EMA Period", "Length of the exponential moving average filter", "Trend");

_stopLossBuyPoints = Param(nameof(StopLossBuyPoints), 50)
.SetDisplay("Buy Stop-Loss (points)", "Protective distance in points for long trades", "Risk");

_trailingStopBuyPoints = Param(nameof(TrailingStopBuyPoints), 10)
.SetDisplay("Buy Trailing (points)", "Trailing stop distance in points for long trades", "Risk");

_stopLossSellPoints = Param(nameof(StopLossSellPoints), 50)
.SetDisplay("Sell Stop-Loss (points)", "Protective distance in points for short trades", "Risk");

_trailingStopSellPoints = Param(nameof(TrailingStopSellPoints), 10)
.SetDisplay("Sell Trailing (points)", "Trailing stop distance in points for short trades", "Risk");

_orderVolume = Param(nameof(OrderVolume), 0.1m)
.SetGreaterThanZero()
.SetDisplay("Order Volume", "Volume for each pending order", "General");

_entryOffsetPoints = Param(nameof(EntryOffsetPoints), 15)
.SetGreaterThanZero()
.SetDisplay("Entry Offset (points)", "Distance in points between the rounded level and the stop entry", "Execution");

_secondaryTakeProfitPoints = Param(nameof(SecondaryTakeProfitPoints), 15)
.SetGreaterThanZero()
.SetDisplay("Second Take-Profit (points)", "Additional distance in points for the extended profit target", "Execution");

_roundingFactor = Param(nameof(RoundingFactor), 100m)
.SetGreaterThanZero()
.SetDisplay("Rounding Factor", "Multiplier used to mimic MathCeil/MathFloor price rounding", "Execution");

_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
.SetDisplay("Candle Type", "Primary candle aggregation used for signal calculations", "Data");
}

/// <summary>
/// EMA period used to define the prevailing trend.
/// </summary>
public int MaPeriod
{
get => _maPeriod.Value;
set => _maPeriod.Value = value;
}

/// <summary>
/// Stop-loss distance for long positions measured in points.
/// </summary>
public int StopLossBuyPoints
{
get => _stopLossBuyPoints.Value;
set => _stopLossBuyPoints.Value = value;
}

/// <summary>
/// Trailing stop distance for long positions in points.
/// </summary>
public int TrailingStopBuyPoints
{
get => _trailingStopBuyPoints.Value;
set => _trailingStopBuyPoints.Value = value;
}

/// <summary>
/// Stop-loss distance for short positions measured in points.
/// </summary>
public int StopLossSellPoints
{
get => _stopLossSellPoints.Value;
set => _stopLossSellPoints.Value = value;
}

/// <summary>
/// Trailing stop distance for short positions in points.
/// </summary>
public int TrailingStopSellPoints
{
get => _trailingStopSellPoints.Value;
set => _trailingStopSellPoints.Value = value;
}

/// <summary>
/// Volume applied to every pending stop order.
/// </summary>
public decimal OrderVolume
{
get => _orderVolume.Value;
set => _orderVolume.Value = value;
}

/// <summary>
/// Distance between the rounded level and the stop entry price.
/// </summary>
public int EntryOffsetPoints
{
get => _entryOffsetPoints.Value;
set => _entryOffsetPoints.Value = value;
}

/// <summary>
/// Extra distance added to the extended take-profit target.
/// </summary>
public int SecondaryTakeProfitPoints
{
get => _secondaryTakeProfitPoints.Value;
set => _secondaryTakeProfitPoints.Value = value;
}

/// <summary>
/// Multiplier used to reproduce the MetaTrader rounding logic.
/// </summary>
public decimal RoundingFactor
{
get => _roundingFactor.Value;
set => _roundingFactor.Value = value;
}

/// <summary>
/// Candle data type processed by the strategy.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

var security = Security ?? throw new InvalidOperationException("Security is not specified.");
var step = security.PriceStep ?? 0m;
if (step <= 0m)
{
if (security.Decimals != null && security.Decimals.Value > 0)
step = (decimal)Math.Pow(10, -security.Decimals.Value);
}

if (step <= 0m)
step = 0.0001m;

_pointValue = step;

_ema = new ExponentialMovingAverage { Length = MaPeriod };

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle)
{
if (candle.State != CandleStates.Finished)
return;

if (_ema is null)
return;

var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
var emaValue = _ema.Process(medianPrice, candle.CloseTime, true).ToDecimal();

if (!_ema.IsFormed)
{
_previousOpen = candle.OpenPrice;
_previousClose = candle.ClosePrice;
_previousEma = emaValue;
return;
}

if (_previousOpen is null || _previousClose is null || _previousEma is null)
{
_previousOpen = candle.OpenPrice;
_previousClose = candle.ClosePrice;
_previousEma = emaValue;
return;
}

CleanupInactiveOrders();

if (!IsFormedAndOnlineAndAllowTrading())
{
_previousOpen = candle.OpenPrice;
_previousClose = candle.ClosePrice;
_previousEma = emaValue;
return;
}

var prevOpen = _previousOpen.Value;
var prevClose = _previousClose.Value;
var prevEma = _previousEma.Value;

var rounding = RoundingFactor;
var buyLevel = Math.Ceiling(prevClose * rounding) / rounding;
var sellLevel = Math.Floor(prevClose * rounding) / rounding;

var entryOffset = EntryOffsetPoints * _pointValue;
var secondaryOffset = SecondaryTakeProfitPoints * _pointValue;
var stopBuyOffset = StopLossBuyPoints * _pointValue;
var stopSellOffset = StopLossSellPoints * _pointValue;

if (prevOpen > prevEma && prevClose > prevEma)
{
PlaceBuyStops(buyLevel, entryOffset, stopBuyOffset, secondaryOffset);
}
else if (prevOpen < prevEma && prevClose < prevEma)
{
PlaceSellStops(sellLevel, entryOffset, stopSellOffset, secondaryOffset);
}

ApplyTrailing(candle.ClosePrice);

_previousOpen = candle.OpenPrice;
_previousClose = candle.ClosePrice;
_previousEma = emaValue;
}

private void PlaceBuyStops(decimal roundedLevel, decimal entryOffset, decimal stopOffset, decimal secondaryOffset)
{
if (_pointValue <= 0m || OrderVolume <= 0m)
return;

var entryPrice = NormalizePrice(roundedLevel - entryOffset);
var stopPrice = NormalizePrice(entryPrice - stopOffset);
var firstTake = NormalizePrice(roundedLevel);
var secondTake = NormalizePrice(roundedLevel + secondaryOffset);

if (entryPrice <= 0m || stopPrice <= 0m || firstTake <= 0m || secondTake <= 0m)
return;

CancelPendingOrders(Sides.Sell);

RemoveMismatchedPendingOrders(Sides.Buy, entryPrice);

var existingPrimary = HasPendingOrder(Sides.Buy, entryPrice, firstTake);
var existingSecondary = HasPendingOrder(Sides.Buy, entryPrice, secondTake);

if (!existingPrimary)
{
var order = BuyStop(OrderVolume, entryPrice);
_pendingOrders[order] = new PendingOrderInfo
{
Side = Sides.Buy,
EntryPrice = entryPrice,
StopPrice = stopPrice,
TakeProfitPrice = firstTake,
Volume = OrderVolume
};
}

if (!existingSecondary)
{
var order = BuyStop(OrderVolume, entryPrice);
_pendingOrders[order] = new PendingOrderInfo
{
Side = Sides.Buy,
EntryPrice = entryPrice,
StopPrice = stopPrice,
TakeProfitPrice = secondTake,
Volume = OrderVolume
};
}

_lastBuyLevel = roundedLevel;
}

private void PlaceSellStops(decimal roundedLevel, decimal entryOffset, decimal stopOffset, decimal secondaryOffset)
{
if (_pointValue <= 0m || OrderVolume <= 0m)
return;

var entryPrice = NormalizePrice(roundedLevel + entryOffset);
var stopPrice = NormalizePrice(entryPrice + stopOffset);
var firstTake = NormalizePrice(roundedLevel);
var secondTake = NormalizePrice(roundedLevel - secondaryOffset);

if (entryPrice <= 0m || stopPrice <= 0m || firstTake <= 0m || secondTake <= 0m)
return;

CancelPendingOrders(Sides.Buy);

RemoveMismatchedPendingOrders(Sides.Sell, entryPrice);

var existingPrimary = HasPendingOrder(Sides.Sell, entryPrice, firstTake);
var existingSecondary = HasPendingOrder(Sides.Sell, entryPrice, secondTake);

if (!existingPrimary)
{
var order = SellStop(OrderVolume, entryPrice);
_pendingOrders[order] = new PendingOrderInfo
{
Side = Sides.Sell,
EntryPrice = entryPrice,
StopPrice = stopPrice,
TakeProfitPrice = firstTake,
Volume = OrderVolume
};
}

if (!existingSecondary)
{
var order = SellStop(OrderVolume, entryPrice);
_pendingOrders[order] = new PendingOrderInfo
{
Side = Sides.Sell,
EntryPrice = entryPrice,
StopPrice = stopPrice,
TakeProfitPrice = secondTake,
Volume = OrderVolume
};
}

_lastSellLevel = roundedLevel;
}

private void ApplyTrailing(decimal closePrice)
{
if (_pointValue <= 0m)
return;

if (Position > 0m && TrailingStopBuyPoints > 0)
{
var distance = TrailingStopBuyPoints * _pointValue;
if (closePrice - PositionPrice >= distance)
{
var candidate = NormalizePrice(closePrice - distance);
if (candidate > 0m && (!_longStopPrice.HasValue || candidate > _longStopPrice.Value))
{
_longStopPrice = candidate;
EnsureStopOrder(true);
}
}
}
else if (Position < 0m && TrailingStopSellPoints > 0)
{
var distance = TrailingStopSellPoints * _pointValue;
if (PositionPrice - closePrice >= distance)
{
var candidate = NormalizePrice(closePrice + distance);
if (candidate > 0m && (!_shortStopPrice.HasValue || candidate < _shortStopPrice.Value))
{
_shortStopPrice = candidate;
EnsureStopOrder(false);
}
}
}
}

private void EnsureStopOrder(bool isLong)
{
var targetPrice = isLong ? _longStopPrice : _shortStopPrice;
if (targetPrice is null)
{
if (isLong)
CancelOrderIfActive(ref _longStopOrder);
else
CancelOrderIfActive(ref _shortStopOrder);
return;
}

var volume = Math.Abs(Position);
if (volume <= 0m)
{
if (isLong)
CancelOrderIfActive(ref _longStopOrder);
else
CancelOrderIfActive(ref _shortStopOrder);
return;
}

var normalizedPrice = NormalizePrice(targetPrice.Value);
if (normalizedPrice <= 0m)
return;

if (isLong)
{
if (_longStopOrder == null)
{
_longStopOrder = SellStop(volume, normalizedPrice);
return;
}

if (_longStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
{
_longStopOrder = null;
EnsureStopOrder(true);
return;
}

if (_longStopOrder.Price != normalizedPrice || _longStopOrder.Volume != volume)
ReRegisterOrder(_longStopOrder, normalizedPrice, volume);
}
else
{
if (_shortStopOrder == null)
{
_shortStopOrder = BuyStop(volume, normalizedPrice);
return;
}

if (_shortStopOrder.State is OrderStates.Done or OrderStates.Failed or OrderStates.Canceled)
{
_shortStopOrder = null;
EnsureStopOrder(false);
return;
}

if (_shortStopOrder.Price != normalizedPrice || _shortStopOrder.Volume != volume)
ReRegisterOrder(_shortStopOrder, normalizedPrice, volume);
}
}

private void CleanupInactiveOrders()
{
foreach (var pair in _pendingOrders.ToArray())
{
if (!IsOrderActive(pair.Key))
_pendingOrders.Remove(pair.Key);
}

foreach (var order in _takeProfitOrders.ToArray())
{
if (!IsOrderActive(order))
_takeProfitOrders.Remove(order);
}

if (_longStopOrder != null && !IsOrderActive(_longStopOrder))
_longStopOrder = null;

if (_shortStopOrder != null && !IsOrderActive(_shortStopOrder))
_shortStopOrder = null;
}

private void CancelPendingOrders(Sides side)
{
foreach (var pair in _pendingOrders.ToArray())
{
if (pair.Value.Side != side)
continue;

CancelOrderIfActive(pair.Key);
_pendingOrders.Remove(pair.Key);
}
}

private void RemoveMismatchedPendingOrders(Sides side, decimal entryPrice)
{
foreach (var pair in _pendingOrders.ToArray())
{
var info = pair.Value;
if (info.Side != side)
continue;

if (info.EntryPrice == entryPrice)
continue;

CancelOrderIfActive(pair.Key);
_pendingOrders.Remove(pair.Key);
}
}

private bool HasPendingOrder(Sides side, decimal entryPrice, decimal takeProfit)
{
foreach (var info in _pendingOrders.Values)
{
if (info.Side == side && info.EntryPrice == entryPrice && info.TakeProfitPrice == takeProfit)
return true;
}

return false;
}

private decimal NormalizePrice(decimal price)
{
if (_pointValue <= 0m)
return price;

var steps = Math.Round(price / _pointValue, MidpointRounding.AwayFromZero);
return steps * _pointValue;
}

/// <inheritdoc />
protected override void OnOwnTradeReceived(MyTrade trade)
{
base.OnOwnTradeReceived(trade);

var order = trade.Order;
if (order == null)
return;

if (_pendingOrders.TryGetValue(order, out var info))
{
_pendingOrders.Remove(order);

var isLong = info.Side == Sides.Buy;
var takeOrder = RegisterTakeProfit(isLong, info.TakeProfitPrice, trade.Trade.Volume);
if (takeOrder != null)
_takeProfitOrders.Add(takeOrder);

if (isLong)
{
var stopCandidate = NormalizePrice(info.StopPrice);
if (stopCandidate > 0m)
{
_longStopPrice = !_longStopPrice.HasValue
? stopCandidate
: Math.Max(_longStopPrice.Value, stopCandidate);
EnsureStopOrder(true);
}
}
else
{
var stopCandidate = NormalizePrice(info.StopPrice);
if (stopCandidate > 0m)
{
_shortStopPrice = !_shortStopPrice.HasValue
? stopCandidate
: Math.Min(_shortStopPrice.Value, stopCandidate);
EnsureStopOrder(false);
}
}

return;
}

if (_takeProfitOrders.Contains(order) && order.State == OrderStates.Done)
{
_takeProfitOrders.Remove(order);

if (Position == 0m)
{
_longStopPrice = null;
_shortStopPrice = null;
CancelOrderIfActive(ref _longStopOrder);
CancelOrderIfActive(ref _shortStopOrder);
}
else if (Position > 0m)
{
EnsureStopOrder(true);
}
else if (Position < 0m)
{
EnsureStopOrder(false);
}
}
}

/// <inheritdoc />
protected override void OnOrderReceived(Order order)
{
base.OnOrderReceived(order);

if (_pendingOrders.ContainsKey(order))
{
if (order.State is OrderStates.Failed or OrderStates.Canceled)
_pendingOrders.Remove(order);
}

if (_takeProfitOrders.Contains(order))
{
if (order.State is OrderStates.Failed or OrderStates.Canceled)
_takeProfitOrders.Remove(order);
}
}

/// <inheritdoc />
protected override void OnPositionReceived(Position position)
{
base.OnPositionReceived(position);

if (Position == 0m)
{
_longStopPrice = null;
_shortStopPrice = null;
CancelOrderIfActive(ref _longStopOrder);
CancelOrderIfActive(ref _shortStopOrder);

foreach (var order in _takeProfitOrders.ToArray())
{
CancelOrderIfActive(order);
_takeProfitOrders.Remove(order);
}
}
else if (Position > 0m)
{
EnsureStopOrder(true);
}
else if (Position < 0m)
{
EnsureStopOrder(false);
}
}

private Order RegisterTakeProfit(bool isLong, decimal price, decimal volume)
{
var normalizedPrice = NormalizePrice(price);
if (normalizedPrice <= 0m || volume <= 0m)
return null;

return isLong
? SellLimit(volume, normalizedPrice)
: BuyLimit(volume, normalizedPrice);
}

private void CancelOrderIfActive(ref Order order)
{
if (order == null)
return;

if (IsOrderActive(order))
CancelOrder(order);

order = null;
}

private void CancelOrderIfActive(Order order)
{
if (IsOrderActive(order))
CancelOrder(order);
}

private static bool IsOrderActive(Order order)
{
return order != null && (order.State == OrderStates.Active || order.State == OrderStates.Pending);
}
}

