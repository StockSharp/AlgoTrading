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
/// Port of the "Adaptive Grid Mt4" expert advisor.
/// Builds ATR-based stop grids around price and expires pending orders after a timer.
/// </summary>
public class AdaptiveGridMt4Strategy : Strategy
{
	private readonly StrategyParam<int> _gridLevels;
	private readonly StrategyParam<int> _timerBars;
	private readonly StrategyParam<decimal> _priceOffsetMultiplier;
	private readonly StrategyParam<decimal> _gridStepMultiplier;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<DataType> _candleType;

	private AverageTrueRange _atr = null!;
	private readonly List<GridOrder> _gridOrders = new();
	private readonly List<Order> _protectionOrders = new();
	private long _barIndex;

	/// <summary>
	/// Number of stop orders per side.
	/// </summary>
	public int GridLevels
	{
		get => _gridLevels.Value;
		set => _gridLevels.Value = value;
	}

	/// <summary>
	/// Maximum lifetime of pending orders measured in candles.
	/// </summary>
	public int TimerBars
	{
		get => _timerBars.Value;
		set => _timerBars.Value = value;
	}

	/// <summary>
	/// Initial offset from the market price expressed in ATR multiples.
	/// </summary>
	public decimal PriceOffsetMultiplier
	{
		get => _priceOffsetMultiplier.Value;
		set => _priceOffsetMultiplier.Value = value;
	}

	/// <summary>
	/// Distance between consecutive grid levels expressed in ATR multiples.
	/// </summary>
	public decimal GridStepMultiplier
	{
		get => _gridStepMultiplier.Value;
		set => _gridStepMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in ATR multiples.
	/// </summary>
	public decimal StopLossMultiplier
	{
		get => _stopLossMultiplier.Value;
		set => _stopLossMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in ATR multiples.
	/// </summary>
	public decimal TakeProfitMultiplier
	{
		get => _takeProfitMultiplier.Value;
		set => _takeProfitMultiplier.Value = value;
	}

	/// <summary>
	/// ATR averaging period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Volume used for each pending order.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Candle type used to drive the grid updates.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AdaptiveGridMt4Strategy"/> class.
	/// </summary>
	public AdaptiveGridMt4Strategy()
	{
		_gridLevels = Param(nameof(GridLevels), 10)
		.SetGreaterThanZero()
		.SetDisplay("Grid Levels", "Number of pending orders placed above and below price", "Grid");

		_timerBars = Param(nameof(TimerBars), 15)
		.SetGreaterThanZero()
		.SetDisplay("Timer Bars", "Maximum number of candles before pending orders expire", "Grid");

		_priceOffsetMultiplier = Param(nameof(PriceOffsetMultiplier), 0.8m)
		.SetGreaterThanZero()
		.SetDisplay("Price Offset", "Initial breakout offset measured in ATR multiples", "Grid");

		_gridStepMultiplier = Param(nameof(GridStepMultiplier), 0.5m)
		.SetGreaterThanZero()
		.SetDisplay("Grid Step", "Distance between grid levels in ATR multiples", "Grid");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 2.4m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss", "Stop-loss distance in ATR multiples", "Risk");

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 2.8m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit", "Take-profit distance in ATR multiples", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "Number of candles used for ATR smoothing", "Indicators");

		_orderVolume = Param(nameof(OrderVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Volume sent with each pending order", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used to trigger grid recalculation", "General");
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
	_gridOrders.Clear();
	_protectionOrders.Clear();
	_barIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
	base.OnStarted(time);

	_atr = new AverageTrueRange
	{
	Length = AtrPeriod
	};

	var subscription = SubscribeCandles(CandleType);
	subscription
	.Bind(_atr, ProcessCandle)
	.Start();

	var area = CreateChartArea();
	if (area != null)
	{
	DrawCandles(area, subscription);
	DrawIndicator(area, _atr);
	DrawOwnTrades(area);
	}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
	if (candle.State != CandleStates.Finished)
	return;

	_barIndex++;

	CancelExpiredOrders();

	if (!IsFormedAndOnlineAndAllowTrading())
	return;

	if (!_atr.IsFormed || atrValue <= 0m)
	return;

	if (Position != 0m)
	return;

	if (HasActiveGridOrders())
	return;

	var price = candle.ClosePrice;
	if (price <= 0m)
	return;

	PlaceGrid(price, atrValue);
	}

	private void PlaceGrid(decimal referencePrice, decimal atrValue)
	{
	var security = Security;
	if (security == null)
	return;

	if (GridLevels <= 0)
	return;

	var offset = atrValue * PriceOffsetMultiplier;
	var step = atrValue * GridStepMultiplier;
	var stopDistance = atrValue * StopLossMultiplier;
	var takeDistance = atrValue * TakeProfitMultiplier;

	var longStart = NormalizePrice(referencePrice + offset);
	var shortStart = NormalizePrice(referencePrice - offset);

	for (var i = 0; i < GridLevels; i++)
	{
	var levelOffset = step * i;

	var buyPrice = NormalizePrice(longStart + levelOffset);
	var sellPrice = NormalizePrice(shortStart - levelOffset);

	var buyStopLoss = stopDistance > 0m ? NormalizePrice(buyPrice - stopDistance) : (decimal?)null;
	var buyTakeProfit = takeDistance > 0m ? NormalizePrice(buyPrice + takeDistance) : (decimal?)null;

	var sellStopLoss = stopDistance > 0m ? NormalizePrice(sellPrice + stopDistance) : (decimal?)null;
	var sellTakeProfit = takeDistance > 0m ? NormalizePrice(sellPrice - takeDistance) : (decimal?)null;

	var buyOrder = BuyStop(OrderVolume, buyPrice);
	_gridOrders.Add(new GridOrder(buyOrder, _barIndex, buyStopLoss, buyTakeProfit));

	var sellOrder = SellStop(OrderVolume, sellPrice);
	_gridOrders.Add(new GridOrder(sellOrder, _barIndex, sellStopLoss, sellTakeProfit));
	}
	}

	private bool HasActiveGridOrders()
	{
	for (var i = 0; i < _gridOrders.Count; i++)
	{
	var order = _gridOrders[i].Order;
	if (order != null && order.State == OrderStates.Active)
	return true;
	}

	return false;
	}

	private void CancelExpiredOrders()
	{
	for (var i = _gridOrders.Count - 1; i >= 0; i--)
	{
	var gridOrder = _gridOrders[i];
	var order = gridOrder.Order;

	if (order == null)
	{
	_gridOrders.RemoveAt(i);
	continue;
	}

	if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Cancelled)
	{
	_gridOrders.RemoveAt(i);
	continue;
	}

	if (TimerBars <= 0)
	continue;

	if (order.State == OrderStates.Active && _barIndex - gridOrder.BarIndex >= TimerBars)
	{
	CancelOrder(order);
	_gridOrders.RemoveAt(i);
	}
	}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
	base.OnOwnTradeReceived(trade);

	var order = trade.Order;
	if (order == null)
	return;

	for (var i = _gridOrders.Count - 1; i >= 0; i--)
	{
	var gridOrder = _gridOrders[i];
	if (gridOrder.Order != order)
	continue;

	_gridOrders.RemoveAt(i);
	RegisterProtectionOrders(gridOrder, trade);
	break;
	}
	}

	private void RegisterProtectionOrders(GridOrder gridOrder, MyTrade trade)
	{
	var security = Security;
	if (security == null)
	return;

	var order = gridOrder.Order;
	if (order == null)
	return;

	var volume = trade.Trade?.Volume ?? order.Volume ?? OrderVolume;
	if (volume <= 0m)
	return;

	if (gridOrder.TakeProfitPrice.HasValue)
	{
	var tpPrice = NormalizePrice(gridOrder.TakeProfitPrice.Value);
	Order tpOrder;
	if (order.Direction == Sides.Buy)
	tpOrder = SellLimit(volume, tpPrice);
	else
	tpOrder = BuyLimit(volume, tpPrice);

	if (tpOrder != null)
	_protectionOrders.Add(tpOrder);
	}

	if (gridOrder.StopLossPrice.HasValue)
	{
	var slPrice = NormalizePrice(gridOrder.StopLossPrice.Value);
	Order slOrder;
	if (order.Direction == Sides.Buy)
	slOrder = SellStop(volume, slPrice);
	else
	slOrder = BuyStop(volume, slPrice);

	if (slOrder != null)
	_protectionOrders.Add(slOrder);
	}
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
	base.OnOrderReceived(order);

	if (order.State != OrderStates.Done && order.State != OrderStates.Failed && order.State != OrderStates.Cancelled)
	return;

	for (var i = _protectionOrders.Count - 1; i >= 0; i--)
	{
	if (_protectionOrders[i] == order)
	{
	_protectionOrders.RemoveAt(i);
	break;
	}
	}
	}

	private decimal NormalizePrice(decimal price)
	{
	var security = Security;
	return security != null ? security.ShrinkPrice(price) : price;
	}

	private sealed class GridOrder
	{
	public GridOrder(Order order, long barIndex, decimal? stopLossPrice, decimal? takeProfitPrice)
	{
	Order = order;
	BarIndex = barIndex;
	StopLossPrice = stopLossPrice;
	TakeProfitPrice = takeProfitPrice;
	}

	public Order Order { get; }
	public long BarIndex { get; }
	public decimal? StopLossPrice { get; }
	public decimal? TakeProfitPrice { get; }
	}
}

