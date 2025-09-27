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
/// Strategy that calculates commission fees for a single discretionary order.
/// Places the configured order once and reports aggregated fees when the strategy stops.
/// </summary>
public class CommissionCalculatorStrategy : Strategy
{
	private readonly StrategyParam<decimal> _quantity;
	private readonly StrategyParam<decimal> _entryPrice;
	private readonly StrategyParam<decimal> _stopLossPrice;
	private readonly StrategyParam<decimal> _takeProfitPrice;
	private readonly StrategyParam<decimal> _commissionRate;
	private readonly StrategyParam<OrderMode> _orderMode;

	private decimal _totalFee;
	private decimal _lastFee;
	private decimal? _initialBalance;
	private bool _orderSent;

	/// <summary>
	/// Available order execution modes.
	/// </summary>
	public enum OrderMode
	{
		/// <summary>
		/// Do not send any order.
		/// </summary>
		None,

		/// <summary>
		/// Send a market buy order.
		/// </summary>
		MarketBuy,

		/// <summary>
		/// Send a market sell order.
		/// </summary>
		MarketSell,

		/// <summary>
		/// Send a buy limit order.
		/// </summary>
		BuyLimit,

		/// <summary>
		/// Send a sell limit order.
		/// </summary>
		SellLimit,

		/// <summary>
		/// Send a buy stop order.
		/// </summary>
		BuyStop,

		/// <summary>
		/// Send a sell stop order.
		/// </summary>
		SellStop,
	}

	/// <summary>
	/// Order quantity.
	/// </summary>
	public decimal Quantity
	{
		get => _quantity.Value;
		set => _quantity.Value = value;
	}

	/// <summary>
	/// Price used for pending orders and for calculating protection distances.
	/// </summary>
	public decimal EntryPrice
	{
		get => _entryPrice.Value;
		set => _entryPrice.Value = value;
	}

	/// <summary>
	/// Stop-loss price that defines the protection distance.
	/// </summary>
	public decimal StopLossPrice
	{
		get => _stopLossPrice.Value;
		set => _stopLossPrice.Value = value;
	}

	/// <summary>
	/// Take-profit price that defines the protection distance.
	/// </summary>
	public decimal TakeProfitPrice
	{
		get => _takeProfitPrice.Value;
		set => _takeProfitPrice.Value = value;
	}

	/// <summary>
	/// Commission rate expressed in percent.
	/// </summary>
	public decimal CommissionRate
	{
		get => _commissionRate.Value;
		set => _commissionRate.Value = value;
	}

	/// <summary>
	/// Selected order execution mode.
	/// </summary>
	public OrderMode Mode
	{
		get => _orderMode.Value;
		set => _orderMode.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CommissionCalculatorStrategy()
	{
		_quantity = Param(nameof(Quantity), 0.001m)
			.SetGreaterThanZero()
			.SetDisplay("Quantity", "Order size to send when the strategy starts", "General");

		_entryPrice = Param(nameof(EntryPrice), 31365m)
			.SetDisplay("Entry Price", "Price used for limit or stop orders", "Trading");

		_stopLossPrice = Param(nameof(StopLossPrice), 31200m)
			.SetDisplay("Stop Loss", "Stop-loss price to calculate protection distance", "Risk Management");

		_takeProfitPrice = Param(nameof(TakeProfitPrice), 32100m)
			.SetDisplay("Take Profit", "Take-profit price to calculate protection distance", "Risk Management");

		_commissionRate = Param(nameof(CommissionRate), 0.04m)
			.SetGreaterThanZero()
			.SetDisplay("Commission Rate %", "Commission rate applied to each executed trade", "General");

		_orderMode = Param(nameof(Mode), OrderMode.None)
			.SetDisplay("Order Mode", "Type of order that should be placed", "Trading");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		// Reset cached values so every new run starts with a clean state.
		_totalFee = 0m;
		_lastFee = 0m;
		_initialBalance = null;
		_orderSent = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Store initial balance snapshot for final reporting.
		_initialBalance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;

		// Configure default volume for helper methods like BuyMarket().
		Volume = Quantity;

		// Configure stop-loss and take-profit distances if valid prices are provided.
		SetupProtection();

		// Place the initial order immediately if a mode is selected.
		PlaceInitialOrder();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		var tradeInfo = trade.Trade;
		if (tradeInfo == null)
			return;

		if (trade.Order.Security != Security)
			return;

		var price = tradeInfo.Price;
		var volume = tradeInfo.Volume;

		if (price <= 0m || volume <= 0m)
			return;

		// Calculate commission using the configured rate.
		var fee = price * volume * CommissionRate / 100m;
		_totalFee += fee;
		_lastFee = fee;

		LogInfo($"Trade executed at {price} for {volume}. Calculated commission: {fee:0.########}");
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		base.OnStopped();

		var currentBalance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue;
		var initial = _initialBalance ?? currentBalance;

		LogInfo($"Initial Balance: {initial:0.########}, Brokerage Fee: {_totalFee:0.########}, Final Balance (including Fee): {(currentBalance ?? 0m) - _totalFee:0.########}");
	}

	private void SetupProtection()
	{
		if (EntryPrice <= 0m)
			return;

		Unit takeProfit = null;
		Unit stopLoss = null;

		var takeDistance = Math.Abs(TakeProfitPrice - EntryPrice);
		if (takeDistance > 0m)
			takeProfit = new Unit(takeDistance, UnitTypes.Absolute);

		var stopDistance = Math.Abs(EntryPrice - StopLossPrice);
		if (stopDistance > 0m)
			stopLoss = new Unit(stopDistance, UnitTypes.Absolute);

		if (takeProfit != null || stopLoss != null)
		{
			// Use market orders for protection to mimic immediate exit when levels are reached.
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss, useMarketOrders: true);
		}
	}

	private void PlaceInitialOrder()
	{
		if (_orderSent)
			return;

		var volume = Volume;

		if (Mode == OrderMode.None || volume <= 0m)
		{
			LogInfo("Order mode is set to None or volume is zero. No order will be sent.");
			return;
		}

		Order order = Mode switch
		{
			OrderMode.MarketBuy => BuyMarket(volume),
			OrderMode.MarketSell => SellMarket(volume),
			OrderMode.BuyLimit => EntryPrice > 0m ? BuyLimit(EntryPrice, volume) : null,
			OrderMode.SellLimit => EntryPrice > 0m ? SellLimit(EntryPrice, volume) : null,
			OrderMode.BuyStop => EntryPrice > 0m ? BuyStop(EntryPrice, volume) : null,
			OrderMode.SellStop => EntryPrice > 0m ? SellStop(EntryPrice, volume) : null,
			_ => null,
		};

		if (order == null)
		{
			LogInfo("Unable to place the configured order. Check EntryPrice and parameters.");
			return;
		}

		_orderSent = true;
		LogInfo($"Initial order sent. Mode: {Mode}, Volume: {volume}, Price: {order.Price:0.########}");
	}
}

