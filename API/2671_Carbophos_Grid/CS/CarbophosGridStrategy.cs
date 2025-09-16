
using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy converted from the Carbophos MetaTrader 5 expert advisor.
/// Places symmetric limit order ladders and manages profit and loss on the aggregated position.
/// </summary>
public class CarbophosGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _maxLoss;
	private readonly StrategyParam<int> _stepPips;
	private readonly StrategyParam<int> _ordersPerSide;
	private readonly StrategyParam<decimal> _orderVolume;

	private decimal _bestBid;
	private decimal _bestAsk;
	private bool _gridPlaced;
	private bool _gridPendingActivation;

	/// <summary>
	/// Floating profit level (in money) that triggers closing of all positions and orders.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Maximum allowed floating loss (in money) before the grid is closed.
	/// </summary>
	public decimal MaxLoss
	{
		get => _maxLoss.Value;
		set => _maxLoss.Value = value;
	}

	/// <summary>
	/// Distance between grid levels expressed in pips.
	/// </summary>
	public int StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Number of limit orders to place above and below the market price.
	/// </summary>
	public int OrdersPerSide
	{
		get => _ordersPerSide.Value;
		set => _ordersPerSide.Value = value;
	}

	/// <summary>
	/// Volume for each limit order registered by the grid.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="CarbophosGridStrategy"/>.
	/// </summary>
	public CarbophosGridStrategy()
	{
		_profitTarget = Param(nameof(ProfitTarget), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Target", "Floating profit target in money", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 50m);

		_maxLoss = Param(nameof(MaxLoss), 150m)
			.SetGreaterThanZero()
			.SetDisplay("Max Loss", "Maximum floating loss before closing", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(50m, 500m, 25m);

		_stepPips = Param(nameof(StepPips), 50)
			.SetGreaterThanZero()
			.SetDisplay("Step (pips)", "Distance between grid levels in pips", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(10, 150, 10);

		_ordersPerSide = Param(nameof(OrdersPerSide), 5)
			.SetGreaterThanZero()
			.SetDisplay("Orders Per Side", "Number of pending orders on each side", "Grid")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume for each pending order", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = 0m;
		_bestAsk = 0m;
		_gridPlaced = false;
		_gridPendingActivation = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to Level1 data to track best bid and ask updates.
		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		// Enable the built-in protection subsystem once.
		StartProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1 == null)
			return;

		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
			_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
			_bestAsk = (decimal)ask;

		// Re-evaluate the grid whenever new price data arrives.
		CheckGridState();
	}

	private void CheckGridState()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var activeOrders = GetActiveOrdersCount();

		if (_gridPendingActivation && activeOrders > 0)
			_gridPendingActivation = false;

		if (Position == 0 && activeOrders == 0)
		{
			if (!_gridPendingActivation)
				_gridPlaced = false;

			if (!_gridPlaced && TryPlaceGrid())
				_gridPlaced = true;

			return;
		}

		// No need to check profit if we do not have an open position yet.
		if (Position == 0)
			return;

		var evaluationPrice = GetEvaluationPrice();
		if (evaluationPrice == 0m)
			return;

		var floatingPnL = CalculateFloatingPnL(evaluationPrice);

		if (floatingPnL >= ProfitTarget)
		{
			ClosePositionsAndOrders("Profit target reached.");
		}
		else if (floatingPnL <= -MaxLoss)
		{
			ClosePositionsAndOrders("Maximum loss reached.");
		}
	}

	private bool TryPlaceGrid()
	{
		if (OrdersPerSide <= 0)
			return false;

		var stepSize = GetGridStep();
		if (stepSize <= 0m)
			return false;

		if (_bestBid <= 0m || _bestAsk <= 0m)
			return false;

		var placed = false;

		for (var i = 1; i <= OrdersPerSide; i++)
		{
			var offset = stepSize * i;
			var sellPrice = _bestBid + offset;
			var buyPrice = _bestAsk - offset;

			// Register sell limit orders above the best bid price.
			SellLimit(OrderVolume, sellPrice);
			placed = true;

			// Register buy limit orders below the best ask price if it stays positive.
			if (buyPrice > 0m)
			{
				BuyLimit(OrderVolume, buyPrice);
				placed = true;
			}
		}

		if (placed)
			_gridPendingActivation = true;

		return placed;
	}

	private decimal GetGridStep()
	{
		var security = Security;
		if (security == null)
			return 0m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 0m;

		var multiplier = (security.Decimals == 3 || security.Decimals == 5) ? 10m : 1m;
		return StepPips * priceStep * multiplier;
	}

	private int GetActiveOrdersCount()
	{
		var count = 0;

		foreach (var order in Orders)
		{
			if (order == null || order.Security != Security)
				continue;

			if (order.State == OrderStates.Active)
				count++;
		}

		return count;
	}

	private decimal GetEvaluationPrice()
	{
		if (Position > 0)
			return _bestBid;

		if (Position < 0)
			return _bestAsk;

		return 0m;
	}

	private decimal CalculateFloatingPnL(decimal marketPrice)
	{
		var security = Security;
		if (security == null)
			return 0m;

		var priceStep = security.PriceStep ?? 0m;
		var stepPrice = security.StepPrice ?? priceStep;

		if (priceStep <= 0m || stepPrice <= 0m || PositionPrice == 0m)
			return 0m;

		var diff = marketPrice - PositionPrice;
		var steps = diff / priceStep;

		return steps * stepPrice * Position;
	}

	private void ClosePositionsAndOrders(string reason)
	{
		if (Position > 0)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		CancelActiveOrders();
		_gridPlaced = false;
		_gridPendingActivation = false;

		LogInfo(reason);
	}
}
