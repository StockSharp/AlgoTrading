using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid strategy that places symmetric limit orders around the current price and pyramids positions when price approaches the latest order.
/// </summary>
public class WaddahAttarWinStrategy : Strategy
{
	private readonly StrategyParam<int> _stepPoints;
	private readonly StrategyParam<decimal> _firstVolume;
	private readonly StrategyParam<decimal> _incrementVolume;
	private readonly StrategyParam<decimal> _minProfit;

	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _lastBuyLimitPrice;
	private decimal _lastSellLimitPrice;
	private decimal _lastBuyLimitVolume;
	private decimal _lastSellLimitVolume;
	private decimal _referenceBalance;
	private bool _hasInitialOrders;

	/// <summary>
	/// Distance in points between the market price and new pending orders.
	/// </summary>
	public int StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Initial volume for the first pair of limit orders.
	/// </summary>
	public decimal FirstVolume
	{
		get => _firstVolume.Value;
		set => _firstVolume.Value = value;
	}

	/// <summary>
	/// Volume increment that is added when new orders are stacked.
	/// </summary>
	public decimal IncrementVolume
	{
		get => _incrementVolume.Value;
		set => _incrementVolume.Value = value;
	}

	/// <summary>
	/// Equity profit target that forces the strategy to close all trades.
	/// </summary>
	public decimal MinProfit
	{
		get => _minProfit.Value;
		set => _minProfit.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public WaddahAttarWinStrategy()
	{
		_stepPoints = Param(nameof(StepPoints), 20)
			.SetGreaterThanZero()
			.SetDisplay("Step (Points)", "Distance from market price to pending orders in points", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 5);

		_firstVolume = Param(nameof(FirstVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("First Volume", "Volume for the initial pending orders", "General");

		_incrementVolume = Param(nameof(IncrementVolume), 0m)
			.SetDisplay("Increment Volume", "Additional volume applied to subsequent grid orders", "General");

		_minProfit = Param(nameof(MinProfit), 910m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Min Profit", "Required equity increase to close all trades", "Risk");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = null;
		_bestAsk = null;
		_lastBuyLimitPrice = 0m;
		_lastSellLimitPrice = 0m;
		_lastBuyLimitVolume = 0m;
		_lastSellLimitVolume = 0m;
		_referenceBalance = 0m;
		_hasInitialOrders = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_referenceBalance = Portfolio?.CurrentValue ?? 0m;

		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();
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

		var equity = Portfolio?.CurrentValue ?? 0m;

		if (MinProfit > 0m && equity >= _referenceBalance + MinProfit && (_hasInitialOrders || Position != 0))
		{
			CancelActiveOrders();
			CloseAll();
			ResetLastOrderInfo();
			_hasInitialOrders = false;
			_referenceBalance = equity;
			return;
		}

		if (!_hasInitialOrders && Position == 0)
		{
			_referenceBalance = equity;
			PlaceInitialOrders(bid, ask);
			return;
		}

		if (!_hasInitialOrders)
			return;

		var priceStep = Security?.PriceStep ?? 0.0001m;
		if (priceStep <= 0m)
			return;

		var stepOffset = StepPoints * priceStep;
		if (stepOffset <= 0m)
			return;

		var proximity = priceStep * 5m;

		if (_lastBuyLimitPrice > 0m && ask - _lastBuyLimitPrice <= proximity)
			PlaceAdditionalBuy(bid, stepOffset);

		if (_lastSellLimitPrice > 0m && _lastSellLimitPrice - bid <= proximity)
			PlaceAdditionalSell(ask, stepOffset);
	}

	private void PlaceInitialOrders(decimal bid, decimal ask)
	{
		if (FirstVolume <= 0m)
			return;

		var priceStep = Security?.PriceStep ?? 0.0001m;
		if (priceStep <= 0m)
			return;

		var stepOffset = StepPoints * priceStep;
		if (stepOffset <= 0m)
			return;

		var buyPrice = NormalizePrice(bid - stepOffset);
		var sellPrice = NormalizePrice(ask + stepOffset);

		var anyPlaced = false;

		var buyOrder = BuyLimit(price: buyPrice, volume: FirstVolume);
		if (buyOrder != null)
		{
			_lastBuyLimitPrice = buyPrice;
			_lastBuyLimitVolume = FirstVolume;
			anyPlaced = true;
		}

		var sellOrder = SellLimit(price: sellPrice, volume: FirstVolume);
		if (sellOrder != null)
		{
			_lastSellLimitPrice = sellPrice;
			_lastSellLimitVolume = FirstVolume;
			anyPlaced = true;
		}

		if (anyPlaced)
			_hasInitialOrders = true;
	}

	private void PlaceAdditionalBuy(decimal bid, decimal stepOffset)
	{
		var volume = _lastBuyLimitVolume + IncrementVolume;
		if (volume <= 0m)
			return;

		var price = NormalizePrice(bid - stepOffset);
		if (price <= 0m)
			return;

		var order = BuyLimit(price: price, volume: volume);
		if (order == null)
			return;

		_lastBuyLimitPrice = price;
		_lastBuyLimitVolume = volume;
	}

	private void PlaceAdditionalSell(decimal ask, decimal stepOffset)
	{
		var volume = _lastBuyLimitVolume + IncrementVolume;
		if (volume <= 0m)
			return;

		var price = NormalizePrice(ask + stepOffset);
		if (price <= 0m)
			return;

		var order = SellLimit(price: price, volume: volume);
		if (order == null)
			return;

		_lastSellLimitPrice = price;
		_lastSellLimitVolume = volume;
	}

	private void ResetLastOrderInfo()
	{
		_lastBuyLimitPrice = 0m;
		_lastSellLimitPrice = 0m;
		_lastBuyLimitVolume = 0m;
		_lastSellLimitVolume = 0m;
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
