using System;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pending order grid strategy that mirrors the classic AntiFragile EA behavior.
/// Places layered stop orders above and below price with martingale style sizing.
/// Applies optional take profit, stop loss, and trailing stop management.
/// </summary>
public class PendingOrderGridStrategy : Strategy
{
	private readonly StrategyParam<decimal> _startingVolume;
	private readonly StrategyParam<decimal> _volumeIncreasePercent;
	private readonly StrategyParam<decimal> _distanceFromPrice;
	private readonly StrategyParam<int> _spaceBetweenTrades;
	private readonly StrategyParam<int> _numberOfTrades;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<bool> _tradeLong;
	private readonly StrategyParam<bool> _tradeShort;

	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _tickSize;
	private decimal _volumeStep;
	private bool _gridInitialized;
	private Order?[] _longGridOrders = Array.Empty<Order?>();
	private Order?[] _shortGridOrders = Array.Empty<Order?>();
	private Order? _stopLossOrder;
	private Order? _takeProfitOrder;
	private decimal? _currentStopPrice;

	/// <summary>
	/// Initial volume used for the first pending order.
	/// </summary>
	public decimal StartingVolume
	{
		get => _startingVolume.Value;
		set => _startingVolume.Value = value;
	}

	/// <summary>
	/// Percentage increase applied to each additional grid order.
	/// </summary>
	public decimal VolumeIncreasePercent
	{
		get => _volumeIncreasePercent.Value;
		set => _volumeIncreasePercent.Value = value;
	}

	/// <summary>
	/// Absolute distance added above or below price before the first order.
	/// </summary>
	public decimal DistanceFromPrice
	{
		get => _distanceFromPrice.Value;
		set => _distanceFromPrice.Value = value;
	}

	/// <summary>
	/// Spacing between consecutive orders expressed in price steps.
	/// </summary>
	public int SpaceBetweenTrades
	{
		get => _spaceBetweenTrades.Value;
		set => _spaceBetweenTrades.Value = value;
	}

	/// <summary>
	/// Total number of orders per direction inside the grid.
	/// </summary>
	public int NumberOfTrades
	{
		get => _numberOfTrades.Value;
		set => _numberOfTrades.Value = value;
	}

	/// <summary>
	/// Take profit distance measured in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance measured in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance measured in price steps.
	/// </summary>
	public int TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Enables the long side of the grid.
	/// </summary>
	public bool TradeLong
	{
		get => _tradeLong.Value;
		set => _tradeLong.Value = value;
	}

	/// <summary>
	/// Enables the short side of the grid.
	/// </summary>
	public bool TradeShort
	{
		get => _tradeShort.Value;
		set => _tradeShort.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PendingOrderGridStrategy"/> class.
	/// </summary>
	public PendingOrderGridStrategy()
	{
		_startingVolume = Param(nameof(StartingVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Starting Volume", "Initial order volume", "Grid");

		_volumeIncreasePercent = Param(nameof(VolumeIncreasePercent), 0.1m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Volume Increase %", "Percent increase applied per grid level", "Grid");

		_distanceFromPrice = Param(nameof(DistanceFromPrice), 0.001m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Distance", "Absolute distance from price to first order", "Grid");

		_spaceBetweenTrades = Param(nameof(SpaceBetweenTrades), 150)
			.SetGreaterThanZero()
			.SetDisplay("Spacing (ticks)", "Number of price steps between orders", "Grid");

		_numberOfTrades = Param(nameof(NumberOfTrades), 200)
			.SetGreaterThanZero()
			.SetDisplay("Orders per side", "Maximum grid orders for each direction", "Grid");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 200)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (ticks)", "Take profit distance in price steps", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 9999)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (ticks)", "Stop loss distance in price steps", "Risk");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 150)
			.SetGreaterOrEqualZero()
			.SetDisplay("Trailing Stop (ticks)", "Trailing distance in price steps", "Risk");

		_tradeLong = Param(nameof(TradeLong), true)
			.SetDisplay("Enable Long Grid", "Place buy stop orders", "Grid");

		_tradeShort = Param(nameof(TradeShort), true)
			.SetDisplay("Enable Short Grid", "Place sell stop orders", "Grid");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset cached market data and order references.
		_bestBid = 0m;
		_bestAsk = 0m;
		_tickSize = 0m;
		_volumeStep = 0m;
		_gridInitialized = false;
		_longGridOrders = Array.Empty<Order?>();
		_shortGridOrders = Array.Empty<Order?>();
		_stopLossOrder = null;
		_takeProfitOrder = null;
		_currentStopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Cache trading steps to round prices and volumes.
		_tickSize = Security?.PriceStep ?? 0m;
		if (_tickSize <= 0m)
			_tickSize = 0.0001m;

		_volumeStep = Security?.VolumeStep ?? 0m;
		if (_volumeStep <= 0m)
			_volumeStep = 0.01m;

		// Listen to order book updates to drive grid placement and trailing.
		SubscribeOrderBook()
			.Bind(ProcessOrderBook)
			.Start();
	}

	private void ProcessOrderBook(QuoteChangeMessage depth)
	{
		// Update the most recent bid and ask prices.
		var bestBid = depth.GetBestBid();
		if (bestBid != null)
			_bestBid = bestBid.Price;

		var bestAsk = depth.GetBestAsk();
		if (bestAsk != null)
			_bestAsk = bestAsk.Price;

		// Allow a new grid only after previous orders are filled or canceled.
		if (_gridInitialized && Position == 0m && CountActiveGridOrders() == 0)
			_gridInitialized = false;

		if (!_gridInitialized)
			TryPlaceGridOrders();

		// Continuously maintain trailing protection.
		UpdateTrailing();
	}

	private void TryPlaceGridOrders()
	{
		if (!TradeLong && !TradeShort)
			return;

		if (_bestBid <= 0m || _bestAsk <= 0m)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var spacing = _tickSize * SpaceBetweenTrades;
		var distance = DistanceFromPrice;

		var longOrders = TradeLong ? new Order?[NumberOfTrades] : Array.Empty<Order?>();
		var shortOrders = TradeShort ? new Order?[NumberOfTrades] : Array.Empty<Order?>();

		for (var index = 1; index <= NumberOfTrades; index++)
		{
			var multiplier = 1m + (index - 1) * VolumeIncreasePercent / 100m;
			var rawVolume = StartingVolume * multiplier;
			var volume = RoundVolume(rawVolume);

			if (volume <= 0m)
				continue;

			if (TradeLong)
			{
				var price = RoundPrice(_bestBid + distance + index * spacing);
				if (price > 0m)
					longOrders[index - 1] = BuyStop(volume, price);
			}

			if (TradeShort)
			{
				var price = RoundPrice(_bestAsk - distance - index * spacing);
				if (price > 0m)
					shortOrders[index - 1] = SellStop(volume, price);
			}
		}

		_longGridOrders = longOrders;
		_shortGridOrders = shortOrders;
		_gridInitialized = true;
	}

	private void UpdateTrailing()
	{
		if (TrailingStopPoints <= 0 || _tickSize <= 0m)
			return;

		var trailingDistance = TrailingStopPoints * _tickSize;

		if (Position > 0m && _bestBid > 0m)
		{
			var entryPrice = PositionAvgPrice;
			if (entryPrice <= 0m)
				return;

			var profit = _bestBid - entryPrice;
			if (profit <= trailingDistance)
				return;

			var newStop = RoundPrice(_bestBid - trailingDistance);

			if (_currentStopPrice is decimal current && newStop <= current)
				return;

			PlaceStopOrder(newStop, Position, true);
		}
		else if (Position < 0m && _bestAsk > 0m)
		{
			var entryPrice = PositionAvgPrice;
			if (entryPrice <= 0m)
				return;

			var profit = entryPrice - _bestAsk;
			if (profit <= trailingDistance)
				return;

			var newStop = RoundPrice(_bestAsk + trailingDistance);

			if (_currentStopPrice is decimal current && newStop >= current)
				return;

			PlaceStopOrder(newStop, Math.Abs(Position), false);
		}
	}

	private void PlaceStopOrder(decimal price, decimal volume, bool forLongPosition)
	{
		if (_stopLossOrder != null)
		{
			var state = _stopLossOrder.State;
			if (state == OrderStates.Active || state == OrderStates.Pending)
				CancelOrder(_stopLossOrder);
		}

		var roundedVolume = RoundVolume(volume);
		if (roundedVolume <= 0m)
			return;

		_stopLossOrder = forLongPosition
			? SellStop(roundedVolume, price)
			: BuyStop(roundedVolume, price);

		_currentStopPrice = price;
	}

	private void SetupLongProtection()
	{
		CancelProtectionOrders();

		var volume = RoundVolume(Position);
		if (volume <= 0m)
			return;

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
			return;

		if (StopLossPoints > 0)
		{
			var stopPrice = RoundPrice(entryPrice - StopLossPoints * _tickSize);
			_stopLossOrder = SellStop(volume, stopPrice);
			_currentStopPrice = stopPrice;
		}

		if (TakeProfitPoints > 0)
		{
			var takePrice = RoundPrice(entryPrice + TakeProfitPoints * _tickSize);
			_takeProfitOrder = SellLimit(volume, takePrice);
		}
	}

	private void SetupShortProtection()
	{
		CancelProtectionOrders();

		var volume = RoundVolume(Math.Abs(Position));
		if (volume <= 0m)
			return;

		var entryPrice = PositionAvgPrice;
		if (entryPrice <= 0m)
			return;

		if (StopLossPoints > 0)
		{
			var stopPrice = RoundPrice(entryPrice + StopLossPoints * _tickSize);
			_stopLossOrder = BuyStop(volume, stopPrice);
			_currentStopPrice = stopPrice;
		}

		if (TakeProfitPoints > 0)
		{
			var takePrice = RoundPrice(entryPrice - TakeProfitPoints * _tickSize);
			_takeProfitOrder = BuyLimit(volume, takePrice);
		}
	}

	private void CancelProtectionOrders()
	{
		if (_stopLossOrder != null)
		{
			var state = _stopLossOrder.State;
			if (state == OrderStates.Active || state == OrderStates.Pending)
				CancelOrder(_stopLossOrder);
			_stopLossOrder = null;
		}

		if (_takeProfitOrder != null)
		{
			var state = _takeProfitOrder.State;
			if (state == OrderStates.Active || state == OrderStates.Pending)
				CancelOrder(_takeProfitOrder);
			_takeProfitOrder = null;
		}

		_currentStopPrice = null;
	}

	private int CountActiveGridOrders()
	{
		var count = 0;

		if (_longGridOrders.Length > 0)
		{
			for (var i = 0; i < _longGridOrders.Length; i++)
			{
				var order = _longGridOrders[i];
				if (order == null)
					continue;

				var state = order.State;
				if (state == OrderStates.Active || state == OrderStates.Pending)
					count++;
			}
		}

		if (_shortGridOrders.Length > 0)
		{
			for (var i = 0; i < _shortGridOrders.Length; i++)
			{
				var order = _shortGridOrders[i];
				if (order == null)
					continue;

				var state = order.State;
				if (state == OrderStates.Active || state == OrderStates.Pending)
					count++;
			}
		}

		return count;
	}

	private decimal RoundPrice(decimal price)
	{
		if (_tickSize <= 0m)
			return price;

		var steps = Math.Round(price / _tickSize, MidpointRounding.AwayFromZero);
		return steps * _tickSize;
	}

	private decimal RoundVolume(decimal volume)
	{
		var absVolume = Math.Abs(volume);
		if (absVolume <= 0m)
			return 0m;

		if (_volumeStep <= 0m)
			return absVolume;

		var steps = Math.Round(absVolume / _volumeStep, MidpointRounding.AwayFromZero);
		if (steps <= 0m)
			steps = 1m;

		return steps * _volumeStep;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		// Configure protection depending on current net position.
		if (Position > 0m)
			SetupLongProtection();
		else if (Position < 0m)
			SetupShortProtection();
		else
			CancelProtectionOrders();
	}
}
