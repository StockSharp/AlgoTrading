namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Manual order management strategy that mirrors the TradingBoxing expert panel.
/// Provides parameter toggles for closing positions, cancelling pending orders and placing new orders.
/// </summary>
public class TradingBoxingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _buyVolume;
	private readonly StrategyParam<decimal> _sellVolume;
	private readonly StrategyParam<decimal> _buyStopVolume;
	private readonly StrategyParam<decimal> _buyLimitVolume;
	private readonly StrategyParam<decimal> _sellStopVolume;
	private readonly StrategyParam<decimal> _sellLimitVolume;
	private readonly StrategyParam<decimal> _buyStopPrice;
	private readonly StrategyParam<decimal> _buyLimitPrice;
	private readonly StrategyParam<decimal> _sellStopPrice;
	private readonly StrategyParam<decimal> _sellLimitPrice;
	private readonly StrategyParam<bool> _closeBuyPositions;
	private readonly StrategyParam<bool> _closeSellPositions;
	private readonly StrategyParam<bool> _deleteBuyStops;
	private readonly StrategyParam<bool> _deleteBuyLimits;
	private readonly StrategyParam<bool> _deleteSellStops;
	private readonly StrategyParam<bool> _deleteSellLimits;
	private readonly StrategyParam<bool> _openBuyMarket;
	private readonly StrategyParam<bool> _openSellMarket;
	private readonly StrategyParam<bool> _placeBuyStop;
	private readonly StrategyParam<bool> _placeBuyLimit;
	private readonly StrategyParam<bool> _placeSellStop;
	private readonly StrategyParam<bool> _placeSellLimit;

	private readonly List<Order> _buyStopOrders = new();
	private readonly List<Order> _buyLimitOrders = new();
	private readonly List<Order> _sellStopOrders = new();
	private readonly List<Order> _sellLimitOrders = new();

	private bool _isRunning;

	/// <summary>
	/// Initializes interactive parameters.
	/// </summary>
	public TradingBoxingStrategy()
	{
		_buyVolume = Param(nameof(BuyVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Volume", "Volume for market buy orders", "Volumes");

		_sellVolume = Param(nameof(SellVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Volume", "Volume for market sell orders", "Volumes");

		_buyStopVolume = Param(nameof(BuyStopVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Stop Volume", "Volume used when placing buy stop orders", "Volumes");

		_buyLimitVolume = Param(nameof(BuyLimitVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Buy Limit Volume", "Volume used when placing buy limit orders", "Volumes");

		_sellStopVolume = Param(nameof(SellStopVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Stop Volume", "Volume used when placing sell stop orders", "Volumes");

		_sellLimitVolume = Param(nameof(SellLimitVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Sell Limit Volume", "Volume used when placing sell limit orders", "Volumes");

		_buyStopPrice = Param(nameof(BuyStopPrice), 0m)
			.SetDisplay("Buy Stop Price", "Price for pending buy stop orders", "Prices");

		_buyLimitPrice = Param(nameof(BuyLimitPrice), 0m)
			.SetDisplay("Buy Limit Price", "Price for pending buy limit orders", "Prices");

		_sellStopPrice = Param(nameof(SellStopPrice), 0m)
			.SetDisplay("Sell Stop Price", "Price for pending sell stop orders", "Prices");

		_sellLimitPrice = Param(nameof(SellLimitPrice), 0m)
			.SetDisplay("Sell Limit Price", "Price for pending sell limit orders", "Prices");

		_closeBuyPositions = Param(nameof(CloseBuyPositions), false)
			.SetDisplay("Close Long Positions", "Set to true to close all long positions", "Actions");

		_closeSellPositions = Param(nameof(CloseSellPositions), false)
			.SetDisplay("Close Short Positions", "Set to true to close all short positions", "Actions");

		_deleteBuyStops = Param(nameof(DeleteBuyStops), false)
			.SetDisplay("Delete Buy Stops", "Cancel active buy stop orders", "Actions");

		_deleteBuyLimits = Param(nameof(DeleteBuyLimits), false)
			.SetDisplay("Delete Buy Limits", "Cancel active buy limit orders", "Actions");

		_deleteSellStops = Param(nameof(DeleteSellStops), false)
			.SetDisplay("Delete Sell Stops", "Cancel active sell stop orders", "Actions");

		_deleteSellLimits = Param(nameof(DeleteSellLimits), false)
			.SetDisplay("Delete Sell Limits", "Cancel active sell limit orders", "Actions");

		_openBuyMarket = Param(nameof(OpenBuyMarket), false)
			.SetDisplay("Open Buy Market", "Send a market buy order with the configured volume", "Actions");

		_openSellMarket = Param(nameof(OpenSellMarket), false)
			.SetDisplay("Open Sell Market", "Send a market sell order with the configured volume", "Actions");

		_placeBuyStop = Param(nameof(PlaceBuyStop), false)
			.SetDisplay("Place Buy Stop", "Place a buy stop order using configured price and volume", "Actions");

		_placeBuyLimit = Param(nameof(PlaceBuyLimit), false)
			.SetDisplay("Place Buy Limit", "Place a buy limit order using configured price and volume", "Actions");

		_placeSellStop = Param(nameof(PlaceSellStop), false)
			.SetDisplay("Place Sell Stop", "Place a sell stop order using configured price and volume", "Actions");

		_placeSellLimit = Param(nameof(PlaceSellLimit), false)
			.SetDisplay("Place Sell Limit", "Place a sell limit order using configured price and volume", "Actions");
	}

	/// <summary>
	/// Gets or sets the market buy volume.
	/// </summary>
	public decimal BuyVolume
	{
		get => _buyVolume.Value;
		set => _buyVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the market sell volume.
	/// </summary>
	public decimal SellVolume
	{
		get => _sellVolume.Value;
		set => _sellVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the buy stop volume.
	/// </summary>
	public decimal BuyStopVolume
	{
		get => _buyStopVolume.Value;
		set => _buyStopVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the buy limit volume.
	/// </summary>
	public decimal BuyLimitVolume
	{
		get => _buyLimitVolume.Value;
		set => _buyLimitVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the sell stop volume.
	/// </summary>
	public decimal SellStopVolume
	{
		get => _sellStopVolume.Value;
		set => _sellStopVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the sell limit volume.
	/// </summary>
	public decimal SellLimitVolume
	{
		get => _sellLimitVolume.Value;
		set => _sellLimitVolume.Value = value;
	}

	/// <summary>
	/// Gets or sets the price for buy stop orders.
	/// </summary>
	public decimal BuyStopPrice
	{
		get => _buyStopPrice.Value;
		set => _buyStopPrice.Value = value;
	}

	/// <summary>
	/// Gets or sets the price for buy limit orders.
	/// </summary>
	public decimal BuyLimitPrice
	{
		get => _buyLimitPrice.Value;
		set => _buyLimitPrice.Value = value;
	}

	/// <summary>
	/// Gets or sets the price for sell stop orders.
	/// </summary>
	public decimal SellStopPrice
	{
		get => _sellStopPrice.Value;
		set => _sellStopPrice.Value = value;
	}

	/// <summary>
	/// Gets or sets the price for sell limit orders.
	/// </summary>
	public decimal SellLimitPrice
	{
		get => _sellLimitPrice.Value;
		set => _sellLimitPrice.Value = value;
	}

	/// <summary>
	/// Requests closing of all long positions.
	/// </summary>
	public bool CloseBuyPositions
	{
		get => _closeBuyPositions.Value;
		set
		{
			if (_closeBuyPositions.Value == value)
				return;

			_closeBuyPositions.Value = value;

			if (value)
				ProcessCloseBuyPositions();
		}
	}

	/// <summary>
	/// Requests closing of all short positions.
	/// </summary>
	public bool CloseSellPositions
	{
		get => _closeSellPositions.Value;
		set
		{
			if (_closeSellPositions.Value == value)
				return;

			_closeSellPositions.Value = value;

			if (value)
				ProcessCloseSellPositions();
		}
	}

	/// <summary>
	/// Requests deletion of buy stop orders.
	/// </summary>
	public bool DeleteBuyStops
	{
		get => _deleteBuyStops.Value;
		set
		{
			if (_deleteBuyStops.Value == value)
				return;

			_deleteBuyStops.Value = value;

			if (value)
				ProcessDeleteBuyStops();
		}
	}

	/// <summary>
	/// Requests deletion of buy limit orders.
	/// </summary>
	public bool DeleteBuyLimits
	{
		get => _deleteBuyLimits.Value;
		set
		{
			if (_deleteBuyLimits.Value == value)
				return;

			_deleteBuyLimits.Value = value;

			if (value)
				ProcessDeleteBuyLimits();
		}
	}

	/// <summary>
	/// Requests deletion of sell stop orders.
	/// </summary>
	public bool DeleteSellStops
	{
		get => _deleteSellStops.Value;
		set
		{
			if (_deleteSellStops.Value == value)
				return;

			_deleteSellStops.Value = value;

			if (value)
				ProcessDeleteSellStops();
		}
	}

	/// <summary>
	/// Requests deletion of sell limit orders.
	/// </summary>
	public bool DeleteSellLimits
	{
		get => _deleteSellLimits.Value;
		set
		{
			if (_deleteSellLimits.Value == value)
				return;

			_deleteSellLimits.Value = value;

			if (value)
				ProcessDeleteSellLimits();
		}
	}

	/// <summary>
	/// Requests a market buy order.
	/// </summary>
	public bool OpenBuyMarket
	{
		get => _openBuyMarket.Value;
		set
		{
			if (_openBuyMarket.Value == value)
				return;

			_openBuyMarket.Value = value;

			if (value)
				ProcessOpenBuyMarket();
		}
	}

	/// <summary>
	/// Requests a market sell order.
	/// </summary>
	public bool OpenSellMarket
	{
		get => _openSellMarket.Value;
		set
		{
			if (_openSellMarket.Value == value)
				return;

			_openSellMarket.Value = value;

			if (value)
				ProcessOpenSellMarket();
		}
	}

	/// <summary>
	/// Requests placing of a buy stop order.
	/// </summary>
	public bool PlaceBuyStop
	{
		get => _placeBuyStop.Value;
		set
		{
			if (_placeBuyStop.Value == value)
				return;

			_placeBuyStop.Value = value;

			if (value)
				ProcessPlaceBuyStop();
		}
	}

	/// <summary>
	/// Requests placing of a buy limit order.
	/// </summary>
	public bool PlaceBuyLimit
	{
		get => _placeBuyLimit.Value;
		set
		{
			if (_placeBuyLimit.Value == value)
				return;

			_placeBuyLimit.Value = value;

			if (value)
				ProcessPlaceBuyLimit();
		}
	}

	/// <summary>
	/// Requests placing of a sell stop order.
	/// </summary>
	public bool PlaceSellStop
	{
		get => _placeSellStop.Value;
		set
		{
			if (_placeSellStop.Value == value)
				return;

			_placeSellStop.Value = value;

			if (value)
				ProcessPlaceSellStop();
		}
	}

	/// <summary>
	/// Requests placing of a sell limit order.
	/// </summary>
	public bool PlaceSellLimit
	{
		get => _placeSellLimit.Value;
		set
		{
			if (_placeSellLimit.Value == value)
				return;

			_placeSellLimit.Value = value;

			if (value)
				ProcessPlaceSellLimit();
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_isRunning = false;
		_buyStopOrders.Clear();
		_buyLimitOrders.Clear();
		_sellStopOrders.Clear();
		_sellLimitOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_isRunning = true;
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		_isRunning = false;
		base.OnStopped();
	}

	private void ProcessCloseBuyPositions()
	{
		try
		{
			if (!EnsureReady("close long positions"))
				return;

			// Close the net long exposure if one exists.
			if (Position > 0)
				SellMarket(Position);
		}
		finally
		{
			_closeBuyPositions.Value = false;
		}
	}

	private void ProcessCloseSellPositions()
	{
		try
		{
			if (!EnsureReady("close short positions"))
				return;

			// Close the net short exposure if one exists.
			if (Position < 0)
				BuyMarket(Math.Abs(Position));
		}
		finally
		{
			_closeSellPositions.Value = false;
		}
	}

	private void ProcessDeleteBuyStops()
	{
		try
		{
			if (!EnsureReady("cancel buy stop orders"))
				return;

			CancelOrders(_buyStopOrders);
		}
		finally
		{
			_deleteBuyStops.Value = false;
		}
	}

	private void ProcessDeleteBuyLimits()
	{
		try
		{
			if (!EnsureReady("cancel buy limit orders"))
				return;

			CancelOrders(_buyLimitOrders);
		}
		finally
		{
			_deleteBuyLimits.Value = false;
		}
	}

	private void ProcessDeleteSellStops()
	{
		try
		{
			if (!EnsureReady("cancel sell stop orders"))
				return;

			CancelOrders(_sellStopOrders);
		}
		finally
		{
			_deleteSellStops.Value = false;
		}
	}

	private void ProcessDeleteSellLimits()
	{
		try
		{
			if (!EnsureReady("cancel sell limit orders"))
				return;

			CancelOrders(_sellLimitOrders);
		}
		finally
		{
			_deleteSellLimits.Value = false;
		}
	}

	private void ProcessOpenBuyMarket()
	{
		try
		{
			if (!EnsureReady("send a market buy order"))
				return;

			if (BuyVolume <= 0)
			{
				LogWarning("Buy volume must be greater than zero.");
				return;
			}

			// Submit a market order with the configured volume.
			BuyMarket(BuyVolume);
		}
		finally
		{
			_openBuyMarket.Value = false;
		}
	}

	private void ProcessOpenSellMarket()
	{
		try
		{
			if (!EnsureReady("send a market sell order"))
				return;

			if (SellVolume <= 0)
			{
				LogWarning("Sell volume must be greater than zero.");
				return;
			}

			// Submit a market order with the configured volume.
			SellMarket(SellVolume);
		}
		finally
		{
			_openSellMarket.Value = false;
		}
	}

	private void ProcessPlaceBuyStop()
	{
		try
		{
			if (!EnsureReady("place a buy stop order"))
				return;

			if (BuyStopVolume <= 0 || BuyStopPrice <= 0)
			{
				LogWarning("Buy stop volume and price must be greater than zero.");
				return;
			}

			// Register a buy stop order and store the reference for future cancellation.
			var order = BuyStop(BuyStopVolume, BuyStopPrice);
			if (order != null)
				_buyStopOrders.Add(order);
		}
		finally
		{
			_placeBuyStop.Value = false;
		}
	}

	private void ProcessPlaceBuyLimit()
	{
		try
		{
			if (!EnsureReady("place a buy limit order"))
				return;

			if (BuyLimitVolume <= 0 || BuyLimitPrice <= 0)
			{
				LogWarning("Buy limit volume and price must be greater than zero.");
				return;
			}

			// Register a buy limit order and store the reference for future cancellation.
			var order = BuyLimit(BuyLimitVolume, BuyLimitPrice);
			if (order != null)
				_buyLimitOrders.Add(order);
		}
		finally
		{
			_placeBuyLimit.Value = false;
		}
	}

	private void ProcessPlaceSellStop()
	{
		try
		{
			if (!EnsureReady("place a sell stop order"))
				return;

			if (SellStopVolume <= 0 || SellStopPrice <= 0)
			{
				LogWarning("Sell stop volume and price must be greater than zero.");
				return;
			}

			// Register a sell stop order and store the reference for future cancellation.
			var order = SellStop(SellStopVolume, SellStopPrice);
			if (order != null)
				_sellStopOrders.Add(order);
		}
		finally
		{
			_placeSellStop.Value = false;
		}
	}

	private void ProcessPlaceSellLimit()
	{
		try
		{
			if (!EnsureReady("place a sell limit order"))
				return;

			if (SellLimitVolume <= 0 || SellLimitPrice <= 0)
			{
				LogWarning("Sell limit volume and price must be greater than zero.");
				return;
			}

			// Register a sell limit order and store the reference for future cancellation.
			var order = SellLimit(SellLimitVolume, SellLimitPrice);
			if (order != null)
				_sellLimitOrders.Add(order);
		}
		finally
		{
			_placeSellLimit.Value = false;
		}
	}

	private void CancelOrders(List<Order> orders)
	{
		foreach (var order in orders)
		{
			if (order == null)
				continue;

			// Cancel only active orders; finished ones will be pruned afterwards.
			if (order.State == OrderStates.Active)
				CancelOrder(order);
		}

		orders.RemoveAll(order => order == null || order.State != OrderStates.Active);
	}

	private bool EnsureReady(string action)
	{
		if (!_isRunning)
		{
			LogWarning($"Cannot {action} because the strategy is not running.");
			return false;
		}

		if (Portfolio == null)
		{
			LogWarning($"Cannot {action} because portfolio is not assigned.");
			return false;
		}

		if (Security == null)
		{
			LogWarning($"Cannot {action} because security is not assigned.");
			return false;
		}

		return true;
	}
}
