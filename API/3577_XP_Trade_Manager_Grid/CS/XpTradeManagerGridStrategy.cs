
namespace StockSharp.Samples.Strategies;

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

public class XpTradeManagerGridStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _maxOrders;
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _takeProfit1Total;
	private readonly StrategyParam<decimal> _takeProfit1Partitive;
	private readonly StrategyParam<decimal> _takeProfit1Offset;
	private readonly StrategyParam<decimal> _takeProfit2;
	private readonly StrategyParam<decimal> _takeProfit3;
	private readonly StrategyParam<decimal> _takeProfit4Total;
	private readonly StrategyParam<decimal> _takeProfit5Total;
	private readonly StrategyParam<decimal> _takeProfit6Total;
	private readonly StrategyParam<decimal> _takeProfit7Total;
	private readonly StrategyParam<decimal> _takeProfit8Total;
	private readonly StrategyParam<decimal> _takeProfit9Total;
	private readonly StrategyParam<decimal> _takeProfit10Total;
	private readonly StrategyParam<decimal> _takeProfit11Total;
	private readonly StrategyParam<decimal> _takeProfit12Total;
	private readonly StrategyParam<decimal> _takeProfit13Total;
	private readonly StrategyParam<decimal> _takeProfit14Total;
	private readonly StrategyParam<decimal> _takeProfit15Total;
	private readonly StrategyParam<Sides> _initialSide;

	private readonly List<GridLeg> _openLegs = new();
	private readonly Dictionary<Sides, CycleStat> _cycleStats = new()
	{
		{ Sides.Buy, new CycleStat() },
		{ Sides.Sell, new CycleStat() }
	};

	private decimal _realizedProfitCurrency;
	private decimal _pipSize;
	private decimal _pipPrice;
	private decimal? _lastBid;
	private decimal? _lastAsk;
	private Order _activeBuyOrder;
	private Order _activeSellOrder;
	private Order _closingBuyOrder;
	private Order _closingSellOrder;
	private int _lastOrdersCount;
	private decimal? _pendingTargetPrice;
	private Sides? _pendingTargetSide;

	// Represents a single executed grid leg.
	private sealed class GridLeg
	{
		public Sides Side { get; set; }
		public decimal Volume { get; set; }
		public decimal EntryPrice { get; set; }
		public decimal? TargetPrice { get; set; }
		public int Index { get; set; }
	}

	// Stores information about the latest completed order #1 cycle.
	private sealed class CycleStat
	{
		public decimal ProfitPips { get; set; }
		public decimal ProfitCurrency { get; set; }
		public decimal ExitPrice { get; set; }
		public bool Completed { get; set; }
	}

	public XpTradeManagerGridStrategy()
	{
		// Initialize all strategy parameters that mirror the original MetaTrader inputs.
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type");

		_orderVolume = Param(nameof(OrderVolume), 0.25m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for every market order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.05m, 1m, 0.05m);

		_maxOrders = Param(nameof(MaxOrders), 15)
			.SetGreaterThanZero()
			.SetDisplay("Max Orders", "Maximum simultaneous grid orders", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(2, 20, 1);

		_stepPoints = Param(nameof(StepPoints), 50)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step (points)", "Price distance in points before adding the next grid order", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(20, 150, 5);

		_riskPercent = Param(nameof(RiskPercent), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Percent", "Maximum floating loss relative to account balance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10m, 200m, 10m);

		_takeProfit1Total = Param(nameof(TakeProfit1Total), 150m).SetDisplay("TP1 Total", null, "Take Profit");
		_takeProfit1Partitive = Param(nameof(TakeProfit1Partitive), 20m).SetDisplay("TP1 Partitive", null, "Take Profit");
		_takeProfit1Offset = Param(nameof(TakeProfit1Offset), 3m).SetDisplay("TP1 Offset", null, "Take Profit");
		_takeProfit2 = Param(nameof(TakeProfit2), 40m).SetDisplay("TP2", null, "Take Profit");
		_takeProfit3 = Param(nameof(TakeProfit3), 50m).SetDisplay("TP3", null, "Take Profit");
		_takeProfit4Total = Param(nameof(TakeProfit4Total), 60m).SetDisplay("TP4 Total", null, "Take Profit");
		_takeProfit5Total = Param(nameof(TakeProfit5Total), 70m).SetDisplay("TP5 Total", null, "Take Profit");
		_takeProfit6Total = Param(nameof(TakeProfit6Total), 80m).SetDisplay("TP6 Total", null, "Take Profit");
		_takeProfit7Total = Param(nameof(TakeProfit7Total), 90m).SetDisplay("TP7 Total", null, "Take Profit");
		_takeProfit8Total = Param(nameof(TakeProfit8Total), 100m).SetDisplay("TP8 Total", null, "Take Profit");
		_takeProfit9Total = Param(nameof(TakeProfit9Total), 120m).SetDisplay("TP9 Total", null, "Take Profit");
		_takeProfit10Total = Param(nameof(TakeProfit10Total), 150m).SetDisplay("TP10 Total", null, "Take Profit");
		_takeProfit11Total = Param(nameof(TakeProfit11Total), 180m).SetDisplay("TP11 Total", null, "Take Profit");
		_takeProfit12Total = Param(nameof(TakeProfit12Total), 200m).SetDisplay("TP12 Total", null, "Take Profit");
		_takeProfit13Total = Param(nameof(TakeProfit13Total), 220m).SetDisplay("TP13 Total", null, "Take Profit");
		_takeProfit14Total = Param(nameof(TakeProfit14Total), 250m).SetDisplay("TP14 Total", null, "Take Profit");
		_takeProfit15Total = Param(nameof(TakeProfit15Total), 300m).SetDisplay("TP15 Total", null, "Take Profit");

		_initialSide = Param(nameof(InitialSide), Sides.Sell)
			.SetDisplay("Initial Side", "Direction of the very first grid order", "Trading");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public int MaxOrders
	{
		get => _maxOrders.Value;
		set => _maxOrders.Value = value;
	}

	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	public decimal TakeProfit1Total
	{
		get => _takeProfit1Total.Value;
		set => _takeProfit1Total.Value = value;
	}

	public decimal TakeProfit1Partitive
	{
		get => _takeProfit1Partitive.Value;
		set => _takeProfit1Partitive.Value = value;
	}

	public decimal TakeProfit1Offset
	{
		get => _takeProfit1Offset.Value;
		set => _takeProfit1Offset.Value = value;
	}

	public decimal TakeProfit2
	{
		get => _takeProfit2.Value;
		set => _takeProfit2.Value = value;
	}

	public decimal TakeProfit3
	{
		get => _takeProfit3.Value;
		set => _takeProfit3.Value = value;
	}

	public decimal TakeProfit4Total
	{
		get => _takeProfit4Total.Value;
		set => _takeProfit4Total.Value = value;
	}

	public decimal TakeProfit5Total
	{
		get => _takeProfit5Total.Value;
		set => _takeProfit5Total.Value = value;
	}

	public decimal TakeProfit6Total
	{
		get => _takeProfit6Total.Value;
		set => _takeProfit6Total.Value = value;
	}

	public decimal TakeProfit7Total
	{
		get => _takeProfit7Total.Value;
		set => _takeProfit7Total.Value = value;
	}

	public decimal TakeProfit8Total
	{
		get => _takeProfit8Total.Value;
		set => _takeProfit8Total.Value = value;
	}

	public decimal TakeProfit9Total
	{
		get => _takeProfit9Total.Value;
		set => _takeProfit9Total.Value = value;
	}

	public decimal TakeProfit10Total
	{
		get => _takeProfit10Total.Value;
		set => _takeProfit10Total.Value = value;
	}

	public decimal TakeProfit11Total
	{
		get => _takeProfit11Total.Value;
		set => _takeProfit11Total.Value = value;
	}

	public decimal TakeProfit12Total
	{
		get => _takeProfit12Total.Value;
		set => _takeProfit12Total.Value = value;
	}

	public decimal TakeProfit13Total
	{
		get => _takeProfit13Total.Value;
		set => _takeProfit13Total.Value = value;
	}

	public decimal TakeProfit14Total
	{
		get => _takeProfit14Total.Value;
		set => _takeProfit14Total.Value = value;
	}

	public decimal TakeProfit15Total
	{
		get => _takeProfit15Total.Value;
		set => _takeProfit15Total.Value = value;
	}

	public Sides InitialSide
	{
		get => _initialSide.Value;
		set => _initialSide.Value = value;
	}

	/// <inheritdoc />
	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		// Subscribe to the selected candle type and open the very first grid order.
		base.OnStarted(time);

		_pipSize = Security?.PriceStep ?? 0.0001m;
		_pipPrice = Security?.StepPrice ?? 1m;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();

		SendInitialOrder();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		// Work only with closed candles to avoid premature decisions.
		if (candle.State != CandleStates.Finished)
			return;

		_lastBid = candle.ClosePrice;
		_lastAsk = candle.ClosePrice;

		ManageTargets(candle);
		ManageGrid(candle.ClosePrice);
		ManageBreakEvenTarget(candle.ClosePrice);
		RiskControl();
		RenewFirstOrder(candle.ClosePrice);
	}

	private void ManageGrid(decimal price)
	{
		// Add new grid legs whenever the price travels far enough from the latest leg.
		if (GetTotalOrders() >= MaxOrders)
			return;

		if (_pipSize <= 0m)
			return;

		var stepDistance = StepPoints * _pipSize;
		if (stepDistance <= 0m)
			return;

		if (_openLegs.Count == 0)
			return;

	var lastBuyLeg = _openLegs.LastOrDefault(l => l.Side == Sides.Buy);
	if (lastBuyLeg != null && price <= lastBuyLeg.EntryPrice - stepDistance)
	{
	TryOpenLeg(Sides.Buy);
	}

	var lastSellLeg = _openLegs.LastOrDefault(l => l.Side == Sides.Sell);
	if (lastSellLeg != null && price >= lastSellLeg.EntryPrice + stepDistance)
	{
	TryOpenLeg(Sides.Sell);
	}
	}

	private void ManageTargets(ICandleMessage candle)
	{
		// Monitor the first three orders and close them when their dedicated targets are touched.
		if (_openLegs.Count == 0)
			return;

	var toClose = new List<GridLeg>();
	foreach (var leg in _openLegs)
	{
	if (leg.TargetPrice is not decimal target)
	continue;

	if (leg.Side == Sides.Buy)
	{
	if (candle.HighPrice >= target)
	toClose.Add(leg);
	}
	else if (leg.Side == Sides.Sell)
	{
	if (candle.LowPrice <= target)
	toClose.Add(leg);
	}
	}

	foreach (var leg in toClose)
	{
	CloseLeg(leg);
	}
	}

	private void ManageBreakEvenTarget(decimal price)
	{
		// Once the ladder becomes large enough, emulate the break-even take-profit cluster from the MQL version.
	var totalOrders = GetTotalOrders();
	if (totalOrders < 4)
	{
	_pendingTargetPrice = null;
	_pendingTargetSide = null;
	_lastOrdersCount = totalOrders;
	return;
	}

	if (totalOrders == _lastOrdersCount && _pendingTargetPrice.HasValue)
	{
	TriggerTarget(price);
	return;
	}

	_lastOrdersCount = totalOrders;

	var breakEven = ComputeBreakEven();
	if (breakEven is null)
	{
	_pendingTargetPrice = null;
	_pendingTargetSide = null;
	return;
	}

	var buyCount = _openLegs.Count(l => l.Side == Sides.Buy);
	var sellCount = _openLegs.Count(l => l.Side == Sides.Sell);
	if (buyCount == sellCount)
	{
	_pendingTargetPrice = null;
	_pendingTargetSide = null;
	return;
	}

	var distancePerOrder = GetTakeProfitTotal(totalOrders) / totalOrders;
	var distancePrice = distancePerOrder * _pipSize;
	if (distancePrice <= 0m)
	{
	_pendingTargetPrice = null;
	_pendingTargetSide = null;
	return;
	}

	_pendingTargetSide = buyCount > sellCount ? Sides.Buy : Sides.Sell;
	_pendingTargetPrice = _pendingTargetSide == Sides.Buy
	? breakEven.Value + distancePrice
	: breakEven.Value - distancePrice;

	TriggerTarget(price);
	}

	private void TriggerTarget(decimal price)
	{
		// Close the entire ladder when the break-even objective gets hit.
	if (_pendingTargetPrice is not decimal target || _pendingTargetSide is null)
	return;

	if (_pendingTargetSide == Sides.Buy)
	{
	if (price >= target)
	{
	CloseAll();
	_pendingTargetPrice = null;
	_pendingTargetSide = null;
	}
	}
	else if (_pendingTargetSide == Sides.Sell)
	{
	if (price <= target)
	{
	CloseAll();
	_pendingTargetPrice = null;
	_pendingTargetSide = null;
	}
	}
	}

	private void CloseAll()
	{
		// Submit flattening orders for both sides.
	if (_closingBuyOrder != null || _closingSellOrder != null)
	return;

	var buyVolume = _openLegs.Where(l => l.Side == Sides.Buy).Sum(l => l.Volume);
	var sellVolume = _openLegs.Where(l => l.Side == Sides.Sell).Sum(l => l.Volume);

	if (buyVolume > 0m)
	_closingSellOrder = SellMarket(buyVolume);

	if (sellVolume > 0m)
	_closingBuyOrder = BuyMarket(sellVolume);
	}

	private void TryOpenLeg(Sides side)
	{
		// Avoid duplicated market orders while one is already in flight.
	if (GetTotalOrders() >= MaxOrders)
	return;

	if (side == Sides.Buy)
	{
	if (_activeBuyOrder != null)
	return;

	_activeBuyOrder = BuyMarket(OrderVolume);
	}
	else
	{
	if (_activeSellOrder != null)
	return;

	_activeSellOrder = SellMarket(OrderVolume);
	}
	}

	private void CloseLeg(GridLeg leg)
	{
		// Send the opposite market order to exit the provided leg volume.
	if (leg.Side == Sides.Buy)
	{
	if (_closingSellOrder == null)
	_closingSellOrder = SellMarket(leg.Volume);
	}
	else
	{
	if (_closingBuyOrder == null)
	_closingBuyOrder = BuyMarket(leg.Volume);
	}
	}

	private int GetTotalOrders()
	{
	return _openLegs.Count;
	}

	private decimal? ComputeBreakEven()
	{
		// Compute the weighted average break-even across all active legs.
	var buySum = _openLegs.Where(l => l.Side == Sides.Buy).Sum(l => l.EntryPrice * l.Volume);
	var buyVolume = _openLegs.Where(l => l.Side == Sides.Buy).Sum(l => l.Volume);
	var sellSum = _openLegs.Where(l => l.Side == Sides.Sell).Sum(l => l.EntryPrice * l.Volume);
	var sellVolume = _openLegs.Where(l => l.Side == Sides.Sell).Sum(l => l.Volume);

	var netVolume = buyVolume - sellVolume;
	if (netVolume == 0m)
	return null;

	var weighted = buySum - sellSum;
	return weighted / netVolume;
	}

	private decimal GetTakeProfitTotal(int count)
	{
	return count switch
	{
	4 => TakeProfit4Total,
	5 => TakeProfit5Total,
	6 => TakeProfit6Total,
	7 => TakeProfit7Total,
	8 => TakeProfit8Total,
	9 => TakeProfit9Total,
	10 => TakeProfit10Total,
	11 => TakeProfit11Total,
	12 => TakeProfit12Total,
	13 => TakeProfit13Total,
	14 => TakeProfit14Total,
	15 => TakeProfit15Total,
	_ => 0m
	};
	}

	private void RiskControl()
	{
		// Trigger an emergency flattening when the floating loss breaches the configured threshold.
	var balance = Portfolio?.BeginValue ?? 0m;
	if (balance <= 0m)
	return;

	var profit = GetFloatingProfit();
	var maxLoss = -balance * (RiskPercent / 100m);

	if (profit < maxLoss)
	{
	CloseAll();
	}
	}

	private decimal GetFloatingProfit()
	{
		// Combine realized and floating profit in account currency.
	var profit = _realizedProfitCurrency;
	var bid = _lastBid ?? 0m;
	var ask = _lastAsk ?? bid;

	foreach (var leg in _openLegs)
	{
	var currentPrice = leg.Side == Sides.Buy ? bid : ask;
	var diff = leg.Side == Sides.Buy ? currentPrice - leg.EntryPrice : leg.EntryPrice - currentPrice;
	profit += ConvertPriceToMoney(diff, leg.Volume);
	}

	return profit;
	}

	private decimal ConvertPriceToMoney(decimal priceDiff, decimal volume)
	{
		// Translate price distance into monetary value using the instrument step settings.
	if (_pipSize <= 0m || _pipPrice <= 0m)
	return 0m;

	var steps = priceDiff / _pipSize;
	return steps * _pipPrice * volume;
	}

	private void RenewFirstOrder(decimal price)
	{
		// Re-open the very first order after a partial cycle finishes but failed to reach the total TP goal.
	if (_pipSize <= 0m)
	return;

	foreach (var side in new[] { Sides.Buy, Sides.Sell })
	{
	var legsOnSide = _openLegs.Count(l => l.Side == side);
	var stat = _cycleStats[side];
	if (legsOnSide > 0 || !stat.Completed)
	continue;

	if (stat.ProfitPips >= TakeProfit1Total)
	{
	stat.Completed = false;
	continue;
	}

	var offset = TakeProfit1Offset * _pipSize;
	if (offset <= 0m)
	{
	stat.Completed = false;
	continue;
	}

	if (side == Sides.Buy)
	{
	if (price <= stat.ExitPrice - offset)
	{
	stat.Completed = false;
	TryOpenLeg(Sides.Buy);
	}
	}
	else
	{
	if (price >= stat.ExitPrice + offset)
	{
	stat.Completed = false;
	TryOpenLeg(Sides.Sell);
	}
	}
	}
	}

	private void SendInitialOrder()
	{
		// Mirror the original expert advisor by firing the very first order immediately.
	if (OrderVolume <= 0m)
	return;

	if (InitialSide == Sides.Buy)
	_activeBuyOrder = BuyMarket(OrderVolume);
	else
	_activeSellOrder = SellMarket(OrderVolume);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		// Track executions to maintain the internal ladder state.
	base.OnOwnTradeReceived(trade);

	if (trade.Order.Security != Security)
	return;

	var volume = trade.Trade.Volume;
	var price = trade.Trade.Price;

	if (trade.Order.Side == Sides.Buy)
	{
	volume = CloseOppositeLegs(Sides.Sell, volume, price);
	if (volume > 0m)
	AddNewLeg(Sides.Buy, volume, price);
	}
	else if (trade.Order.Side == Sides.Sell)
	{
	volume = CloseOppositeLegs(Sides.Buy, volume, price);
	if (volume > 0m)
	AddNewLeg(Sides.Sell, volume, price);
	}
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		// Reset cached handles when orders finish processing.
	base.OnOrderChanged(order);

	if (order.Security != Security)
	return;

	if (order == _activeBuyOrder && order.State is OrderStates.Done or OrderStates.Cancelled or OrderStates.Failed)
	_activeBuyOrder = null;

	if (order == _activeSellOrder && order.State is OrderStates.Done or OrderStates.Cancelled or OrderStates.Failed)
	_activeSellOrder = null;

	if (order == _closingBuyOrder && order.State is OrderStates.Done or OrderStates.Cancelled or OrderStates.Failed)
	_closingBuyOrder = null;

	if (order == _closingSellOrder && order.State is OrderStates.Done or OrderStates.Cancelled or OrderStates.Failed)
	_closingSellOrder = null;
	}

	private decimal CloseOppositeLegs(Sides sideToClose, decimal volume, decimal fillPrice)
	{
		// Allocate the fill volume to the opposite legs first to account for hedged portfolios.
	if (volume <= 0m)
	return 0m;

	var legs = _openLegs.Where(l => l.Side == sideToClose).ToList();
	foreach (var leg in legs)
	{
	if (volume <= 0m)
	break;

	var closing = Math.Min(leg.Volume, volume);
	var diff = leg.Side == Sides.Buy ? fillPrice - leg.EntryPrice : leg.EntryPrice - fillPrice;
	_realizedProfitCurrency += ConvertPriceToMoney(diff, closing);
	leg.Volume -= closing;
	volume -= closing;

	if (leg.Volume <= 0m)
	{
	if (leg.Index == 1)
	{
	var stat = _cycleStats[leg.Side];
	stat.ProfitPips = Math.Abs(diff / (_pipSize <= 0m ? 1m : _pipSize));
	stat.ProfitCurrency = _realizedProfitCurrency;
	stat.ExitPrice = fillPrice;
	stat.Completed = true;
	}

	_openLegs.Remove(leg);
	}
	}

	return volume;
	}

	private void AddNewLeg(Sides side, decimal volume, decimal price)
	{
		// Store the new exposure so the manager can evaluate grid conditions later.
	var index = _openLegs.Count(l => l.Side == side) + 1;
	var leg = new GridLeg
	{
	Side = side,
	Volume = volume,
	EntryPrice = price,
	Index = index,
	TargetPrice = GetInitialTarget(side, index, price)
	};

	_openLegs.Add(leg);
	}

	private decimal? GetInitialTarget(Sides side, int index, decimal entryPrice)
	{
		// Only the first three orders have explicit TP levels in the original code.
	if (_pipSize <= 0m)
	return null;

	decimal distance = index switch
	{
	1 => TakeProfit1Partitive,
	2 => TakeProfit2,
	3 => TakeProfit3,
	_ => 0m
	};

	if (distance <= 0m)
	return null;

	distance *= _pipSize;
	return side == Sides.Buy ? entryPrice + distance : entryPrice - distance;
	}
}

