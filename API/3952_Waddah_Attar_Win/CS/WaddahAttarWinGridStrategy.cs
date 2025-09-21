using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy converted from the "Waddah Attar Win" MetaTrader 4 expert advisor.
/// Places paired limit orders around the market, pyramids positions with an optional volume increment,
/// and closes the entire exposure once the floating profit target is achieved.
/// </summary>
public class WaddahAttarWinGridStrategy : Strategy
{
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<decimal> _firstVolume;
	private readonly StrategyParam<decimal> _incrementVolume;
	private readonly StrategyParam<decimal> _minProfit;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _lastBuyLimitPrice;
	private decimal _lastSellLimitPrice;
	private decimal _lastBuyVolume;
	private decimal _lastSellVolume;
	private decimal _referenceBalance;
	private bool _initialOrdersPlaced;

	/// <summary>
	/// Distance in price points between consecutive grid levels.
	/// </summary>
	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Volume for the very first pair of pending orders.
	/// </summary>
	public decimal FirstVolume
	{
		get => _firstVolume.Value;
		set => _firstVolume.Value = value;
	}

	/// <summary>
	/// Volume increment applied to each newly stacked order.
	/// </summary>
	public decimal IncrementVolume
	{
		get => _incrementVolume.Value;
		set => _incrementVolume.Value = value;
	}

	/// <summary>
	/// Floating profit target in account currency that closes all positions and orders.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public WaddahAttarWinGridStrategy()
	{
		_stepPoints = Param(nameof(StepPoints), 120)
		.SetGreaterThanZero()
		.SetDisplay("Step (Points)", "Distance between grid levels in points", "Grid")
		.SetCanOptimize(true)
		.SetOptimize(20, 400, 10);

		_firstVolume = Param(nameof(FirstVolume), 0.1m)
		.SetGreaterThanZero()
		.SetDisplay("First Volume", "Volume for the initial pending orders", "Trading");

		_incrementVolume = Param(nameof(IncrementVolume), 0m)
		.SetDisplay("Increment Volume", "Additional volume added when stacking new orders", "Trading");

		_minProfit = Param(nameof(MinProfit), 450m)
		.SetNotNegative()
		.SetDisplay("Min Profit", "Floating profit target in account currency", "Risk");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = null;
		_bestAsk = null;
		_lastBuyLimitPrice = 0m;
		_lastSellLimitPrice = 0m;
		_lastBuyVolume = 0m;
		_lastSellVolume = 0m;
		_referenceBalance = 0m;
		_initialOrdersPlaced = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_referenceBalance = Portfolio?.CurrentValue ?? 0m;

		SubscribeOrderBook()
		.Bind(ProcessOrderBook)
		.Start();

		StartProtection();
	}

	private void ProcessOrderBook(IOrderBookMessage depth)
	{
		var bestBid = depth.GetBestBid();
		if (bestBid != null)
		_bestBid = bestBid.Price;

		var bestAsk = depth.GetBestAsk();
		if (bestAsk != null)
		_bestAsk = bestAsk.Price;

		ProcessTrading();
	}

	private void ProcessTrading()
	{
		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
		return;

		var hasActiveOrders = HasActiveOrders();

		if (!hasActiveOrders && Position == 0m)
		{
			if (_initialOrdersPlaced)
			{
				_initialOrdersPlaced = false;
				ResetTracking();
			}

			_referenceBalance = Portfolio?.CurrentValue ?? _referenceBalance;
		}

		var priceStep = Security?.PriceStep ?? 0m;
		if (priceStep <= 0m)
		return;

		var stepOffset = StepPoints * priceStep;
		if (stepOffset <= 0m)
		return;

		var floatingProfit = (Portfolio?.CurrentValue ?? 0m) - _referenceBalance;

		if (MinProfit > 0m && floatingProfit >= MinProfit && (_initialOrdersPlaced || Position != 0m))
		{
			CancelAllOrders();
			CloseAll("Equity profit target reached.");
			_referenceBalance = Portfolio?.CurrentValue ?? _referenceBalance;
			_initialOrdersPlaced = false;
			ResetTracking();
			return;
		}

		if (!_initialOrdersPlaced && !hasActiveOrders && Position == 0m)
		{
			PlaceInitialOrders(bid, ask, stepOffset);
			return;
		}

		if (!_initialOrdersPlaced)
		return;

		var proximity = priceStep * 5m;

		if (_lastBuyLimitPrice > 0m && ask - _lastBuyLimitPrice <= proximity)
		PlaceAdditionalBuy(stepOffset);

		if (_lastSellLimitPrice > 0m && _lastSellLimitPrice - bid <= proximity)
		PlaceAdditionalSell(stepOffset);
	}

	private void PlaceInitialOrders(decimal bid, decimal ask, decimal stepOffset)
	{
		if (FirstVolume <= 0m)
		return;

		var buyPrice = NormalizePrice(ask - stepOffset);
		var sellPrice = NormalizePrice(bid + stepOffset);

		var buyOrder = BuyLimit(price: buyPrice, volume: FirstVolume);
		var sellOrder = SellLimit(price: sellPrice, volume: FirstVolume);

		var anyPlaced = false;

		if (buyOrder != null)
		{
			_lastBuyLimitPrice = buyPrice;
			_lastBuyVolume = FirstVolume;
			anyPlaced = true;
		}

		if (sellOrder != null)
		{
			_lastSellLimitPrice = sellPrice;
			_lastSellVolume = FirstVolume;
			anyPlaced = true;
		}

		if (anyPlaced)
		{
			_initialOrdersPlaced = true;
			_referenceBalance = Portfolio?.CurrentValue ?? _referenceBalance;
		}
	}

	private void PlaceAdditionalBuy(decimal stepOffset)
	{
		var volume = _lastBuyVolume + IncrementVolume;
		if (volume <= 0m)
		return;

		var price = NormalizePrice(_lastBuyLimitPrice - stepOffset);
		if (price <= 0m)
		return;

		var order = BuyLimit(price: price, volume: volume);
		if (order == null)
		return;

		_lastBuyLimitPrice = price;
		_lastBuyVolume = volume;
	}

	private void PlaceAdditionalSell(decimal stepOffset)
	{
		var volume = _lastSellVolume + IncrementVolume;
		if (volume <= 0m)
		return;

		var price = NormalizePrice(_lastSellLimitPrice + stepOffset);
		if (price <= 0m)
		return;

		var order = SellLimit(price: price, volume: volume);
		if (order == null)
		return;

		_lastSellLimitPrice = price;
		_lastSellVolume = volume;
	}

	private void CancelAllOrders()
	{
		foreach (var order in Orders)
		{
			if (order == null || order.Security != Security)
			continue;

			if (IsOrderActive(order))
			CancelOrder(order);
		}
	}

	private bool HasActiveOrders()
	{
		foreach (var order in Orders)
		{
			if (order == null || order.Security != Security)
			continue;

			if (IsOrderActive(order))
			return true;
		}

		return false;
	}

	private static bool IsOrderActive(Order order)
	{
		return order.State == OrderStates.Active
		|| order.State == OrderStates.Pending
		|| order.State == OrderStates.Suspended;
	}

	private void ResetTracking()
	{
		_lastBuyLimitPrice = 0m;
		_lastSellLimitPrice = 0m;
		_lastBuyVolume = 0m;
		_lastSellVolume = 0m;
	}

	private decimal NormalizePrice(decimal price)
	{
		var priceStep = Security?.PriceStep;
		if (priceStep == null || priceStep == 0m)
		return price;

		var steps = Math.Round(price / priceStep.Value, MidpointRounding.AwayFromZero);
		return steps * priceStep.Value;
	}
}
