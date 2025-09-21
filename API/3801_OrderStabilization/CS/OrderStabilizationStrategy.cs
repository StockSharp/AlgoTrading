using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the hjueiisyx8lp2o379e expert advisor focused on stabilization exits.
/// Places paired stop orders around the market and closes trades once price stagnates or a fixed profit target is reached.
/// </summary>
public class OrderStabilizationStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _orderDistancePoints;
	private readonly StrategyParam<decimal> _profitThreshold;
	private readonly StrategyParam<decimal> _absoluteFixation;
	private readonly StrategyParam<decimal> _stabilizationPoints;
	private readonly StrategyParam<int> _expirationMinutes;
	private readonly StrategyParam<DataType> _candleType;

	private Order? _buyStopOrder;
	private Order? _sellStopOrder;
	private DateTimeOffset? _buyExpiry;
	private DateTimeOffset? _sellExpiry;

	private decimal? _previousBody;
	private decimal? _previousPreviousBody;

	private decimal _tickSize;
	private decimal _tickValue;

	/// <summary>
	/// Trading volume in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

/// <summary>
/// Distance from the current price to the pending stop orders expressed in MetaTrader points.
/// </summary>
public decimal OrderDistancePoints
{
	get => _orderDistancePoints.Value;
	set => _orderDistancePoints.Value = value;
}

/// <summary>
/// Minimum profit in account currency required before stabilization exits are allowed.
/// </summary>
public decimal ProfitThreshold
{
get => _profitThreshold.Value;
set => _profitThreshold.Value = value;
}

/// <summary>
/// Absolute profit level in account currency that forces trade liquidation.
/// </summary>
public decimal AbsoluteFixation
{
get => _absoluteFixation.Value;
set => _absoluteFixation.Value = value;
}

/// <summary>
/// Maximum candle body size (in points) considered a stabilization signal.
/// </summary>
public decimal StabilizationPoints
{
get => _stabilizationPoints.Value;
set => _stabilizationPoints.Value = value;
}

/// <summary>
/// Lifetime of pending stop orders in minutes.
/// </summary>
public int ExpirationMinutes
{
get => _expirationMinutes.Value;
set => _expirationMinutes.Value = value;
}

/// <summary>
/// Candle type used to evaluate stabilization conditions.
/// </summary>
public DataType CandleType
{
get => _candleType.Value;
set => _candleType.Value = value;
}

/// <summary>
/// Initializes a new instance of <see cref="OrderStabilizationStrategy"/>.
/// </summary>
public OrderStabilizationStrategy()
{
_orderVolume = Param(nameof(OrderVolume), 0.1m)
.SetGreaterThanZero()
.SetDisplay("Volume", "Order volume in lots", "Trading");

_orderDistancePoints = Param(nameof(OrderDistancePoints), 20m)
.SetGreaterThanZero()
.SetDisplay("Order distance", "Offset for stop orders in MetaTrader points", "Trading");

_profitThreshold = Param(nameof(ProfitThreshold), -2m)
.SetDisplay("Profit threshold", "Minimum profit before stabilization exits", "Risk")
.SetCanOptimize(true);

_absoluteFixation = Param(nameof(AbsoluteFixation), 30m)
.SetDisplay("Absolute fixation", "Profit level that always closes the trade", "Risk")
.SetCanOptimize(true);

_stabilizationPoints = Param(nameof(StabilizationPoints), 25m)
.SetGreaterThanZero()
.SetDisplay("Stabilization", "Maximum candle body size treated as flat market", "Filters");

_expirationMinutes = Param(nameof(ExpirationMinutes), 20)
.SetDisplay("Expiration", "Lifetime of pending orders in minutes", "Trading")
.SetCanOptimize(true);

_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
.SetDisplay("Candle type", "Timeframe used to evaluate stabilization", "Data")
.SetCanOptimize(true);
}

/// <inheritdoc />
protected override void OnStarted(DateTimeOffset time)
{
base.OnStarted(time);

Volume = OrderVolume;

_tickSize = Security.PriceStep ?? 1m;
_tickValue = Security.StepPrice ?? _tickSize;

var subscription = SubscribeCandles(CandleType);
subscription
.Bind(ProcessCandle)
.Start();
}

private void ProcessCandle(ICandleMessage candle, decimal _)
{
if (candle.State != CandleStates.Finished)
return;

UpdatePendingOrdersLifetime(candle);

if (_previousBody is null)
{
	// Store the first completed candle to build the stabilization history.
	_previousBody = candle.ClosePrice - candle.OpenPrice;
	EnsurePendingOrders(candle);
	return;
}

if (Position == 0)
{
EnsurePendingOrders(candle);
}
else
{
ManageOpenPosition(candle);
}

_previousPreviousBody = _previousBody;
_previousBody = candle.ClosePrice - candle.OpenPrice;
}

private void EnsurePendingOrders(ICandleMessage candle)
{
if (!IsFormedAndOnlineAndAllowTrading())
return;

CleanupInactiveOrder(ref _buyStopOrder, ref _buyExpiry);
CleanupInactiveOrder(ref _sellStopOrder, ref _sellExpiry);

if (Position != 0)
return;

var step = _tickSize <= 0 ? 1m : _tickSize;
var offset = OrderDistancePoints * step;
var expiration = ExpirationMinutes > 0 ? candle.CloseTime + TimeSpan.FromMinutes(ExpirationMinutes) : (DateTimeOffset?)null;

if (_buyStopOrder is null)
{
	// Place the long stop entry above the current market.
	_buyStopOrder = BuyStop(candle.ClosePrice + offset, Volume);
	_buyExpiry = expiration;
}

if (_sellStopOrder is null)
{
// Place the short stop entry below the current market.
_sellStopOrder = SellStop(candle.ClosePrice - offset, Volume);
_sellExpiry = expiration;
}
}

private void UpdatePendingOrdersLifetime(ICandleMessage candle)
{
if (ExpirationMinutes <= 0)
return;

if (_buyStopOrder != null && _buyStopOrder.State == OrderStates.Active && _buyExpiry is DateTimeOffset buyExpiry && candle.CloseTime >= buyExpiry)
{
	CancelStrategyOrder(ref _buyStopOrder, ref _buyExpiry);
}

if (_sellStopOrder != null && _sellStopOrder.State == OrderStates.Active && _sellExpiry is DateTimeOffset sellExpiry && candle.CloseTime >= sellExpiry)
{
CancelStrategyOrder(ref _sellStopOrder, ref _sellExpiry);
}
}

private void ManageOpenPosition(ICandleMessage candle)
{
var currentVolume = Math.Abs(Position);
var entryPrice = PositionAvgPrice;

if (currentVolume <= 0 || entryPrice == 0)
return;

var step = _tickSize <= 0 ? 1m : _tickSize;
var stepValue = _tickValue <= 0 ? step : _tickValue;
var priceDiff = Position > 0 ? candle.ClosePrice - entryPrice : entryPrice - candle.ClosePrice;
var profit = priceDiff / step * stepValue * currentVolume;

var stabilizationLimit = StabilizationPoints * step;
var lastBody = _previousBody.HasValue ? Math.Abs(_previousBody.Value) : decimal.MaxValue;
var prevBody = _previousPreviousBody.HasValue ? Math.Abs(_previousPreviousBody.Value) : decimal.MaxValue;

var exitByProfit = profit > ProfitThreshold && lastBody <= stabilizationLimit;
var exitByTwoSmallCandles = lastBody <= stabilizationLimit && prevBody <= stabilizationLimit;
var exitByAbsoluteProfit = profit >= AbsoluteFixation;

if (Position > 0)
{
	if (exitByProfit || exitByTwoSmallCandles || exitByAbsoluteProfit)
	{
		// Close long trades and drop the remaining protective sell stop.
		SellMarket(currentVolume);
		CancelStrategyOrder(ref _sellStopOrder, ref _sellExpiry);
	}
}
else if (Position < 0)
{
if (exitByProfit || exitByTwoSmallCandles || exitByAbsoluteProfit)
{
	// Close short trades and drop the remaining protective buy stop.
	BuyMarket(currentVolume);
	CancelStrategyOrder(ref _buyStopOrder, ref _buyExpiry);
}
}
}

private void CancelStrategyOrder(ref Order? order, ref DateTimeOffset? expiry)
{
if (order == null)
{
	expiry = null;
	return;
}

if (order.State == OrderStates.Active)
CancelOrder(order);

order = null;
expiry = null;
}

private void CleanupInactiveOrder(ref Order? order, ref DateTimeOffset? expiry)
{
if (order != null && order.State != OrderStates.Active)
{
	order = null;
	expiry = null;
}
}

/// <inheritdoc />
protected override void OnOwnTradeReceived(MyTrade trade)
{
base.OnOwnTradeReceived(trade);

if (trade.Order == null || trade.Order.Security != Security)
return;

if (trade.Order == _buyStopOrder)
{
	// Long stop filled - clear the handle so it can be recreated later.
	_buyStopOrder = null;
	_buyExpiry = null;
}
else if (trade.Order == _sellStopOrder)
{
// Short stop filled - clear the handle so it can be recreated later.
_sellStopOrder = null;
_sellExpiry = null;
}
}

/// <inheritdoc />
protected override void OnPositionChanged(decimal delta)
{
base.OnPositionChanged(delta);

if (Position != 0)
return;

// Ensure there are no leftover pending orders once the position is fully closed.
CancelStrategyOrder(ref _buyStopOrder, ref _buyExpiry);
CancelStrategyOrder(ref _sellStopOrder, ref _sellExpiry);
}
}
