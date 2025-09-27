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
/// Port of the TraderToolEA utility that converts the MetaTrader control panel into StockSharp commands.
/// The strategy exposes button-like parameters for manual order placement, grid management, and order cleanup.
/// It also implements virtual stop-loss, take-profit, trailing stop, and break-even guards driven by level1 quotes.
/// </summary>
public class TraderToolEaStrategy : Strategy
{
	private readonly StrategyParam<bool> _useAutoVolume;
	private readonly StrategyParam<decimal> _riskFactor;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _distancePips;
	private readonly StrategyParam<int> _pendingLayers;
	private readonly StrategyParam<bool> _deleteOrphans;

	private readonly StrategyParam<bool> _enableStopLoss;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<bool> _enableTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPips;
	private readonly StrategyParam<bool> _enableBreakEven;
	private readonly StrategyParam<decimal> _breakEvenTriggerPips;
	private readonly StrategyParam<decimal> _breakEvenLockPips;

	private readonly StrategyParam<bool> _openBuyRequest;
	private readonly StrategyParam<bool> _openSellRequest;
	private readonly StrategyParam<bool> _placeBuyStopRequest;
	private readonly StrategyParam<bool> _placeSellStopRequest;
	private readonly StrategyParam<bool> _placeStopGridRequest;
	private readonly StrategyParam<bool> _placeBuyLimitRequest;
	private readonly StrategyParam<bool> _placeSellLimitRequest;
	private readonly StrategyParam<bool> _placeLimitGridRequest;
	private readonly StrategyParam<bool> _closeBuyRequest;
	private readonly StrategyParam<bool> _closeSellRequest;
	private readonly StrategyParam<bool> _closeAllRequest;
	private readonly StrategyParam<bool> _deleteBuyStopsRequest;
	private readonly StrategyParam<bool> _deleteSellStopsRequest;
	private readonly StrategyParam<bool> _deleteAllStopsRequest;
	private readonly StrategyParam<bool> _deleteBuyLimitsRequest;
	private readonly StrategyParam<bool> _deleteSellLimitsRequest;
	private readonly StrategyParam<bool> _deleteAllLimitsRequest;

	private decimal _pipSize;
	private decimal? _bestBid;
	private decimal? _bestAsk;

	private decimal? _longTrailingPrice;
	private decimal? _shortTrailingPrice;
	private bool _longBreakEvenActive;
	private bool _shortBreakEvenActive;
	private decimal _longBreakEvenPrice;
	private decimal _shortBreakEvenPrice;

	private readonly HashSet<Order> _buyStopOrders = new();
	private readonly HashSet<Order> _sellStopOrders = new();
	private readonly HashSet<Order> _buyLimitOrders = new();
	private readonly HashSet<Order> _sellLimitOrders = new();

	/// <summary>
	/// Enables portfolio based volume calculation.
	/// </summary>
	public bool UseAutoVolume
	{
		get => _useAutoVolume.Value;
		set => _useAutoVolume.Value = value;
	}

	/// <summary>
	/// Risk multiplier applied to portfolio balance when <see cref="UseAutoVolume"/> is enabled.
	/// </summary>
	public decimal RiskFactor
	{
		get => _riskFactor.Value;
		set => _riskFactor.Value = value;
	}

	/// <summary>
	/// Manual lot size used for market and pending orders when auto volume is disabled.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Distance between layered pending orders expressed in pips.
	/// </summary>
	public decimal DistancePips
	{
		get => _distancePips.Value;
		set => _distancePips.Value = value;
	}

	/// <summary>
	/// Number of additional pending levels created per command.
	/// </summary>
	public int PendingLayers
	{
		get => _pendingLayers.Value;
		set => _pendingLayers.Value = value;
	}

	/// <summary>
	/// Automatically cancels unmatched pending orders after executions.
	/// </summary>
	public bool DeleteOrphans
	{
		get => _deleteOrphans.Value;
		set => _deleteOrphans.Value = value;
	}

	/// <summary>
	/// Enables fixed stop-loss control in pips.
	/// </summary>
	public bool EnableStopLoss
	{
		get => _enableStopLoss.Value;
		set => _enableStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Enables fixed take-profit control in pips.
	/// </summary>
	public bool EnableTakeProfit
	{
		get => _enableTakeProfit.Value;
		set => _enableTakeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables trailing stop monitoring.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in pips.
	/// </summary>
	public decimal TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Enables break-even protection.
	/// </summary>
	public bool EnableBreakEven
	{
		get => _enableBreakEven.Value;
		set => _enableBreakEven.Value = value;
	}

	/// <summary>
	/// Profit distance required before the break-even stop activates (in pips).
	/// </summary>
	public decimal BreakEvenTriggerPips
	{
		get => _breakEvenTriggerPips.Value;
		set => _breakEvenTriggerPips.Value = value;
	}

	/// <summary>
	/// Offset applied to the break-even stop once activated (in pips).
	/// </summary>
	public decimal BreakEvenLockPips
	{
		get => _breakEvenLockPips.Value;
		set => _breakEvenLockPips.Value = value;
	}

	/// <summary>
	/// Sends a market buy order when toggled to <c>true</c>.
	/// </summary>
	public bool OpenBuyRequest
	{
		get => _openBuyRequest.Value;
		set => _openBuyRequest.Value = value;
	}

	/// <summary>
	/// Sends a market sell order when toggled to <c>true</c>.
	/// </summary>
	public bool OpenSellRequest
	{
		get => _openSellRequest.Value;
		set => _openSellRequest.Value = value;
	}

	/// <summary>
	/// Places a grid of buy stop orders above the current ask price.
	/// </summary>
	public bool PlaceBuyStopRequest
	{
		get => _placeBuyStopRequest.Value;
		set => _placeBuyStopRequest.Value = value;
	}

	/// <summary>
	/// Places a grid of sell stop orders below the current bid price.
	/// </summary>
	public bool PlaceSellStopRequest
	{
		get => _placeSellStopRequest.Value;
		set => _placeSellStopRequest.Value = value;
	}

	/// <summary>
	/// Places both buy and sell stop grids at once.
	/// </summary>
	public bool PlaceStopGridRequest
	{
		get => _placeStopGridRequest.Value;
		set => _placeStopGridRequest.Value = value;
	}

	/// <summary>
	/// Places a grid of buy limit orders below the current bid price.
	/// </summary>
	public bool PlaceBuyLimitRequest
	{
		get => _placeBuyLimitRequest.Value;
		set => _placeBuyLimitRequest.Value = value;
	}

	/// <summary>
	/// Places a grid of sell limit orders above the current ask price.
	/// </summary>
	public bool PlaceSellLimitRequest
	{
		get => _placeSellLimitRequest.Value;
		set => _placeSellLimitRequest.Value = value;
	}

	/// <summary>
	/// Places both buy and sell limit grids.
	/// </summary>
	public bool PlaceLimitGridRequest
	{
		get => _placeLimitGridRequest.Value;
		set => _placeLimitGridRequest.Value = value;
	}

	/// <summary>
	/// Closes all long positions using a market sell order.
	/// </summary>
	public bool CloseBuyRequest
	{
		get => _closeBuyRequest.Value;
		set => _closeBuyRequest.Value = value;
	}

	/// <summary>
	/// Closes all short positions using a market buy order.
	/// </summary>
	public bool CloseSellRequest
	{
		get => _closeSellRequest.Value;
		set => _closeSellRequest.Value = value;
	}

	/// <summary>
	/// Closes every open position regardless of direction.
	/// </summary>
	public bool CloseAllRequest
	{
		get => _closeAllRequest.Value;
		set => _closeAllRequest.Value = value;
	}

	/// <summary>
	/// Cancels tracked buy stop orders.
	/// </summary>
	public bool DeleteBuyStopsRequest
	{
		get => _deleteBuyStopsRequest.Value;
		set => _deleteBuyStopsRequest.Value = value;
	}

	/// <summary>
	/// Cancels tracked sell stop orders.
	/// </summary>
	public bool DeleteSellStopsRequest
	{
		get => _deleteSellStopsRequest.Value;
		set => _deleteSellStopsRequest.Value = value;
	}

	/// <summary>
	/// Cancels all tracked stop orders.
	/// </summary>
	public bool DeleteAllStopsRequest
	{
		get => _deleteAllStopsRequest.Value;
		set => _deleteAllStopsRequest.Value = value;
	}

	/// <summary>
	/// Cancels tracked buy limit orders.
	/// </summary>
	public bool DeleteBuyLimitsRequest
	{
		get => _deleteBuyLimitsRequest.Value;
		set => _deleteBuyLimitsRequest.Value = value;
	}

	/// <summary>
	/// Cancels tracked sell limit orders.
	/// </summary>
	public bool DeleteSellLimitsRequest
	{
		get => _deleteSellLimitsRequest.Value;
		set => _deleteSellLimitsRequest.Value = value;
	}

	/// <summary>
	/// Cancels all tracked limit orders.
	/// </summary>
	public bool DeleteAllLimitsRequest
	{
		get => _deleteAllLimitsRequest.Value;
		set => _deleteAllLimitsRequest.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters that emulate the MQL panel switches.
	/// </summary>
	public TraderToolEaStrategy()
	{
		_useAutoVolume = Param(nameof(UseAutoVolume), false)
		.SetDisplay("Use Auto Volume", "Derive order volume from portfolio balance.", "Volume");

		_riskFactor = Param(nameof(RiskFactor), 1m)
		.SetDisplay("Risk Factor", "Multiplier applied to balance when auto volume is enabled.", "Volume");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
		.SetDisplay("Order Volume", "Manual lot size used for new orders.", "Volume");

		_distancePips = Param(nameof(DistancePips), 50m)
		.SetDisplay("Distance (pips)", "Spacing between layered pending orders.", "Pending Orders");

		_pendingLayers = Param(nameof(PendingLayers), 1)
		.SetDisplay("Layers", "Number of additional pending orders per command.", "Pending Orders");

		_deleteOrphans = Param(nameof(DeleteOrphans), true)
		.SetDisplay("Delete Orphans", "Automatically remove unpaired pending orders.", "Pending Orders");

		_enableStopLoss = Param(nameof(EnableStopLoss), true)
		.SetDisplay("Enable Stop Loss", "Attach a fixed stop-loss to open positions.", "Protection");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
		.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips.", "Protection");

		_enableTakeProfit = Param(nameof(EnableTakeProfit), true)
		.SetDisplay("Enable Take Profit", "Attach a fixed take-profit target.", "Protection");

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
		.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips.", "Protection");

		_enableTrailingStop = Param(nameof(EnableTrailingStop), false)
		.SetDisplay("Enable Trailing", "Activate virtual trailing stop management.", "Protection");

		_trailingStopPips = Param(nameof(TrailingStopPips), 20m)
		.SetDisplay("Trailing (pips)", "Trailing stop distance expressed in pips.", "Protection");

		_enableBreakEven = Param(nameof(EnableBreakEven), false)
		.SetDisplay("Enable Break-Even", "Lock profit once price advances by the trigger distance.", "Protection");

		_breakEvenTriggerPips = Param(nameof(BreakEvenTriggerPips), 20m)
		.SetDisplay("Break-Even Trigger", "Profit distance before the break-even stop activates.", "Protection");

		_breakEvenLockPips = Param(nameof(BreakEvenLockPips), 10m)
		.SetDisplay("Break-Even Lock", "Offset added to the entry price when break-even activates.", "Protection");

		_openBuyRequest = Param(nameof(OpenBuyRequest), false)
		.SetDisplay("Open Buy", "Send a market buy order (resets after execution).", "Manual Commands")
		.SetCanOptimize(false);

		_openSellRequest = Param(nameof(OpenSellRequest), false)
		.SetDisplay("Open Sell", "Send a market sell order (resets after execution).", "Manual Commands")
		.SetCanOptimize(false);

		_placeBuyStopRequest = Param(nameof(PlaceBuyStopRequest), false)
		.SetDisplay("Place Buy Stops", "Place a layered buy stop grid above the ask price.", "Manual Commands")
		.SetCanOptimize(false);

		_placeSellStopRequest = Param(nameof(PlaceSellStopRequest), false)
		.SetDisplay("Place Sell Stops", "Place a layered sell stop grid below the bid price.", "Manual Commands")
		.SetCanOptimize(false);

		_placeStopGridRequest = Param(nameof(PlaceStopGridRequest), false)
		.SetDisplay("Place Stop Grid", "Place buy and sell stop grids simultaneously.", "Manual Commands")
		.SetCanOptimize(false);

		_placeBuyLimitRequest = Param(nameof(PlaceBuyLimitRequest), false)
		.SetDisplay("Place Buy Limits", "Place a layered buy limit grid below the bid price.", "Manual Commands")
		.SetCanOptimize(false);

		_placeSellLimitRequest = Param(nameof(PlaceSellLimitRequest), false)
		.SetDisplay("Place Sell Limits", "Place a layered sell limit grid above the ask price.", "Manual Commands")
		.SetCanOptimize(false);

		_placeLimitGridRequest = Param(nameof(PlaceLimitGridRequest), false)
		.SetDisplay("Place Limit Grid", "Place buy and sell limit grids simultaneously.", "Manual Commands")
		.SetCanOptimize(false);

		_closeBuyRequest = Param(nameof(CloseBuyRequest), false)
		.SetDisplay("Close Buys", "Close all open long positions.", "Manual Commands")
		.SetCanOptimize(false);

		_closeSellRequest = Param(nameof(CloseSellRequest), false)
		.SetDisplay("Close Sells", "Close all open short positions.", "Manual Commands")
		.SetCanOptimize(false);

		_closeAllRequest = Param(nameof(CloseAllRequest), false)
		.SetDisplay("Close All", "Close every open position.", "Manual Commands")
		.SetCanOptimize(false);

		_deleteBuyStopsRequest = Param(nameof(DeleteBuyStopsRequest), false)
		.SetDisplay("Delete Buy Stops", "Cancel tracked buy stop orders.", "Manual Commands")
		.SetCanOptimize(false);

		_deleteSellStopsRequest = Param(nameof(DeleteSellStopsRequest), false)
		.SetDisplay("Delete Sell Stops", "Cancel tracked sell stop orders.", "Manual Commands")
		.SetCanOptimize(false);

		_deleteAllStopsRequest = Param(nameof(DeleteAllStopsRequest), false)
		.SetDisplay("Delete All Stops", "Cancel every tracked stop order.", "Manual Commands")
		.SetCanOptimize(false);

		_deleteBuyLimitsRequest = Param(nameof(DeleteBuyLimitsRequest), false)
		.SetDisplay("Delete Buy Limits", "Cancel tracked buy limit orders.", "Manual Commands")
		.SetCanOptimize(false);

		_deleteSellLimitsRequest = Param(nameof(DeleteSellLimitsRequest), false)
		.SetDisplay("Delete Sell Limits", "Cancel tracked sell limit orders.", "Manual Commands")
		.SetCanOptimize(false);

		_deleteAllLimitsRequest = Param(nameof(DeleteAllLimitsRequest), false)
		.SetDisplay("Delete All Limits", "Cancel every tracked limit order.", "Manual Commands")
		.SetCanOptimize(false);
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

		_bestBid = null;
		_bestAsk = null;
		_longTrailingPrice = null;
		_shortTrailingPrice = null;
		_longBreakEvenActive = false;
		_shortBreakEvenActive = false;
		_longBreakEvenPrice = 0m;
		_shortBreakEvenPrice = 0m;
		_buyStopOrders.Clear();
		_sellStopOrders.Clear();
		_buyLimitOrders.Clear();
		_sellLimitOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var security = Security ?? throw new InvalidOperationException("Security is not specified.");
		if (Portfolio == null)
		throw new InvalidOperationException("Portfolio is not specified.");

		_pipSize = CalculatePipSize(security);

		SubscribeLevel1()
		.Bind(ProcessLevel1)
		.Start();
	}

	private static decimal CalculatePipSize(Security security)
	{
		var step = security.PriceStep ?? security.MinStep ?? 0m;
		if (step <= 0m)
		step = 0.0001m;

		var decimals = security.Decimals ?? 0;
		var multiplier = decimals == 3 || decimals == 5 ? 10m : 1m;
		var pip = step * multiplier;

		return pip > 0m ? pip : step;
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid))
		_bestBid = (decimal)bid;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask))
		_bestAsk = (decimal)ask;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		ProcessRequests();
		RemoveOrphanOrders();
		UpdateLongProtection();
		UpdateShortProtection();
	}

	private void ProcessRequests()
	{
		if (OpenBuyRequest)
		{
			TrySendMarketOrder(Sides.Buy);
			OpenBuyRequest = false;
		}

		if (OpenSellRequest)
		{
			TrySendMarketOrder(Sides.Sell);
			OpenSellRequest = false;
		}

		if (PlaceBuyStopRequest)
		{
			TryPlaceStopOrders(Sides.Buy);
			PlaceBuyStopRequest = false;
		}

		if (PlaceSellStopRequest)
		{
			TryPlaceStopOrders(Sides.Sell);
			PlaceSellStopRequest = false;
		}

		if (PlaceStopGridRequest)
		{
			var buyPlaced = TryPlaceStopOrders(Sides.Buy);
			var sellPlaced = TryPlaceStopOrders(Sides.Sell);

			if (!buyPlaced && !sellPlaced)
			LogWarning("Stop grid request skipped because both sides failed to place.");

			PlaceStopGridRequest = false;
		}

		if (PlaceBuyLimitRequest)
		{
			TryPlaceLimitOrders(Sides.Buy);
			PlaceBuyLimitRequest = false;
		}

		if (PlaceSellLimitRequest)
		{
			TryPlaceLimitOrders(Sides.Sell);
			PlaceSellLimitRequest = false;
		}

		if (PlaceLimitGridRequest)
		{
			var buyPlaced = TryPlaceLimitOrders(Sides.Buy);
			var sellPlaced = TryPlaceLimitOrders(Sides.Sell);

			if (!buyPlaced && !sellPlaced)
			LogWarning("Limit grid request skipped because both sides failed to place.");

			PlaceLimitGridRequest = false;
		}

		if (CloseBuyRequest)
		{
			CloseLongPositions();
			CloseBuyRequest = false;
		}

		if (CloseSellRequest)
		{
			CloseShortPositions();
			CloseSellRequest = false;
		}

		if (CloseAllRequest)
		{
			CloseLongPositions();
			CloseShortPositions();
			CloseAllRequest = false;
		}

		if (DeleteBuyStopsRequest)
		{
			CancelOrders(_buyStopOrders);
			DeleteBuyStopsRequest = false;
		}

		if (DeleteSellStopsRequest)
		{
			CancelOrders(_sellStopOrders);
			DeleteSellStopsRequest = false;
		}

		if (DeleteAllStopsRequest)
		{
			CancelOrders(_buyStopOrders);
			CancelOrders(_sellStopOrders);
			DeleteAllStopsRequest = false;
		}

		if (DeleteBuyLimitsRequest)
		{
			CancelOrders(_buyLimitOrders);
			DeleteBuyLimitsRequest = false;
		}

		if (DeleteSellLimitsRequest)
		{
			CancelOrders(_sellLimitOrders);
			DeleteSellLimitsRequest = false;
		}

		if (DeleteAllLimitsRequest)
		{
			CancelOrders(_buyLimitOrders);
			CancelOrders(_sellLimitOrders);
			DeleteAllLimitsRequest = false;
		}
	}

	private void CloseLongPositions()
	{
		if (Position <= 0m)
		return;

		SellMarket(Math.Abs(Position));
	}

	private void CloseShortPositions()
	{
		if (Position >= 0m)
		return;

		BuyMarket(Math.Abs(Position));
	}

	private bool TrySendMarketOrder(Sides side)
	{
		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		{
			LogWarning("Skipped market order because calculated volume is non-positive.");
			return false;
		}

		if (side == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);

		return true;
	}

	private bool TryPlaceStopOrders(Sides side)
	{
		var reference = side == Sides.Buy ? _bestAsk : _bestBid;
		if (reference is not decimal price || price <= 0m)
		{
			LogWarning("Skip stop placement because best price is unavailable.");
			return false;
		}

		var distance = DistancePips * _pipSize;
		if (distance <= 0m)
		distance = _pipSize;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		{
			LogWarning("Skip stop placement because volume is non-positive.");
			return false;
		}

		var layers = Math.Max(1, PendingLayers);
		for (var i = 0; i < layers; i++)
		{
			var offset = distance * (i + 1);
			var rawPrice = side == Sides.Buy ? price + offset : price - offset;
			var aligned = AlignPrice(rawPrice, side != Sides.Sell);

			if (aligned <= 0m)
			continue;

			var order = side == Sides.Buy
			? BuyStop(volume, aligned)
			: SellStop(volume, aligned);

			TrackStopOrder(order, side);
		}

		return true;
	}

	private bool TryPlaceLimitOrders(Sides side)
	{
		var reference = side == Sides.Buy ? _bestBid : _bestAsk;
		if (reference is not decimal price || price <= 0m)
		{
			LogWarning("Skip limit placement because best price is unavailable.");
			return false;
		}

		var distance = DistancePips * _pipSize;
		if (distance <= 0m)
		distance = _pipSize;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
		{
			LogWarning("Skip limit placement because volume is non-positive.");
			return false;
		}

		var layers = Math.Max(1, PendingLayers);
		for (var i = 0; i < layers; i++)
		{
			var offset = distance * (i + 1);
			var rawPrice = side == Sides.Buy ? price - offset : price + offset;
			var aligned = AlignPrice(rawPrice, side == Sides.Sell);

			if (aligned <= 0m)
			continue;

			var order = side == Sides.Buy
			? BuyLimit(volume, aligned)
			: SellLimit(volume, aligned);

			TrackLimitOrder(order, side);
		}

		return true;
	}

	private decimal CalculateOrderVolume()
	{
		var security = Security ?? throw new InvalidOperationException("Security is not specified.");
		decimal volume;

		if (UseAutoVolume)
		{
			var balance = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
			var lotSize = security.LotSize ?? 1m;
			if (lotSize <= 0m)
			lotSize = 1m;

			volume = balance > 0m ? balance / lotSize * RiskFactor : 0m;

			if (volume <= 0m)
			volume = OrderVolume;
		}
		else
		{
			volume = OrderVolume;
		}

		var step = security.VolumeStep ?? 0m;
		if (step > 0m)
		volume = Math.Round(volume / step, MidpointRounding.AwayFromZero) * step;

		var minVolume = security.MinVolume ?? 0m;
		var maxVolume = security.MaxVolume ?? 0m;

		if (minVolume > 0m && volume < minVolume)
		volume = minVolume;

		if (maxVolume > 0m && volume > maxVolume)
		volume = maxVolume;

		if (volume <= 0m && minVolume > 0m)
		volume = minVolume;

		return volume;
	}

	private decimal AlignPrice(decimal price, bool roundUp)
	{
		var step = Security?.PriceStep ?? Security?.MinStep ?? 0m;
		if (step <= 0m)
		return price;

		var multiplier = price / step;
		var rounded = roundUp
		? Math.Ceiling(multiplier) * step
		: Math.Floor(multiplier) * step;

		return rounded;
	}

	private void TrackStopOrder(Order order, Sides side)
	{
		if (order is null)
		return;

		if (side == Sides.Buy)
		_buyStopOrders.Add(order);
		else
		_sellStopOrders.Add(order);
	}

	private void TrackLimitOrder(Order order, Sides side)
	{
		if (order is null)
		return;

		if (side == Sides.Buy)
		_buyLimitOrders.Add(order);
		else
		_sellLimitOrders.Add(order);
	}

	private void CancelOrders(ICollection<Order> orders)
	{
		foreach (var order in orders.ToArray())
		{
			if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Canceled)
			{
				orders.Remove(order);
				continue;
			}

			CancelOrder(order);
		}
	}

	private void RemoveOrphanOrders()
	{
		if (!DeleteOrphans)
		return;

		BalancePairs(_buyStopOrders, _sellStopOrders);
		BalancePairs(_buyLimitOrders, _sellLimitOrders);
	}

	private void BalancePairs(ICollection<Order> primary, ICollection<Order> secondary)
	{
		var activePrimary = CountActive(primary);
		var activeSecondary = CountActive(secondary);

		if (activePrimary == activeSecondary)
		return;

		if (activePrimary > activeSecondary)
		CancelExtra(primary, activePrimary - activeSecondary);
		else
		CancelExtra(secondary, activeSecondary - activePrimary);
	}

	private static int CountActive(ICollection<Order> orders)
	{
		return orders.Count(order => order.State != OrderStates.Done && order.State != OrderStates.Failed && order.State != OrderStates.Canceled);
	}

	private void CancelExtra(ICollection<Order> orders, int count)
	{
		if (count <= 0)
		return;

		foreach (var order in orders.ToArray())
		{
			if (count <= 0)
			break;

			if (order.State == OrderStates.Done || order.State == OrderStates.Failed || order.State == OrderStates.Canceled)
			{
				orders.Remove(order);
				continue;
			}

			CancelOrder(order);
			count--;
		}
	}

	private void UpdateLongProtection()
	{
		if (Position <= 0m)
		{
			_longTrailingPrice = null;
			_longBreakEvenActive = false;
			return;
		}

		if (_bestBid is not decimal bid || bid <= 0m)
		return;

		var entryPrice = Position.AveragePrice ?? 0m;
		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;
		var trailingDistance = TrailingStopPips * _pipSize;
		var breakEvenTrigger = BreakEvenTriggerPips * _pipSize;
		var breakEvenLock = BreakEvenLockPips * _pipSize;

		if (EnableStopLoss && stopDistance > 0m)
		{
			var stopPrice = entryPrice - stopDistance;
			if (bid <= stopPrice)
			{
				SellMarket(Math.Abs(Position));
				ResetLongProtection();
				return;
			}
		}

		if (EnableTakeProfit && takeDistance > 0m)
		{
			var takePrice = entryPrice + takeDistance;
			if (bid >= takePrice)
			{
				SellMarket(Math.Abs(Position));
				ResetLongProtection();
				return;
			}
		}

		if (EnableBreakEven && breakEvenTrigger > 0m && !_longBreakEvenActive && bid >= entryPrice + breakEvenTrigger)
		{
			_longBreakEvenPrice = entryPrice + breakEvenLock;
			_longBreakEvenActive = true;
		}

		if (_longBreakEvenActive && bid <= _longBreakEvenPrice)
		{
			SellMarket(Math.Abs(Position));
			ResetLongProtection();
			return;
		}

		if (EnableTrailingStop && trailingDistance > 0m)
		{
			var candidate = bid - trailingDistance;
			if (candidate > entryPrice)
			{
				if (!_longTrailingPrice.HasValue || candidate > _longTrailingPrice.Value)
				_longTrailingPrice = candidate;

				if (_longTrailingPrice.HasValue && bid <= _longTrailingPrice.Value)
				{
					SellMarket(Math.Abs(Position));
					ResetLongProtection();
				}
			}
		}
		else
		{
			_longTrailingPrice = null;
		}
	}

	private void UpdateShortProtection()
	{
		if (Position >= 0m)
		{
			_shortTrailingPrice = null;
			_shortBreakEvenActive = false;
			return;
		}

		if (_bestAsk is not decimal ask || ask <= 0m)
		return;

		var entryPrice = Position.AveragePrice ?? 0m;
		var stopDistance = StopLossPips * _pipSize;
		var takeDistance = TakeProfitPips * _pipSize;
		var trailingDistance = TrailingStopPips * _pipSize;
		var breakEvenTrigger = BreakEvenTriggerPips * _pipSize;
		var breakEvenLock = BreakEvenLockPips * _pipSize;

		if (EnableStopLoss && stopDistance > 0m)
		{
			var stopPrice = entryPrice + stopDistance;
			if (ask >= stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortProtection();
				return;
			}
		}

		if (EnableTakeProfit && takeDistance > 0m)
		{
			var takePrice = entryPrice - takeDistance;
			if (ask <= takePrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortProtection();
				return;
			}
		}

		if (EnableBreakEven && breakEvenTrigger > 0m && !_shortBreakEvenActive && ask <= entryPrice - breakEvenTrigger)
		{
			_shortBreakEvenPrice = entryPrice - breakEvenLock;
			_shortBreakEvenActive = true;
		}

		if (_shortBreakEvenActive && ask >= _shortBreakEvenPrice)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortProtection();
			return;
		}

		if (EnableTrailingStop && trailingDistance > 0m)
		{
			var candidate = ask + trailingDistance;
			if (candidate < entryPrice)
			{
				if (!_shortTrailingPrice.HasValue || candidate < _shortTrailingPrice.Value)
				_shortTrailingPrice = candidate;

				if (_shortTrailingPrice.HasValue && ask >= _shortTrailingPrice.Value)
				{
					BuyMarket(Math.Abs(Position));
					ResetShortProtection();
				}
			}
		}
		else
		{
			_shortTrailingPrice = null;
		}
	}

	private void ResetLongProtection()
	{
		_longTrailingPrice = null;
		_longBreakEvenActive = false;
		_longBreakEvenPrice = 0m;
	}

	private void ResetShortProtection()
	{
		_shortTrailingPrice = null;
		_shortBreakEvenActive = false;
		_shortBreakEvenPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order.State != OrderStates.Done && order.State != OrderStates.Failed && order.State != OrderStates.Canceled)
		return;

		_buyStopOrders.Remove(order);
		_sellStopOrders.Remove(order);
		_buyLimitOrders.Remove(order);
		_sellLimitOrders.Remove(order);
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade.Order.Side == Sides.Buy)
		ResetLongProtection();
		else if (trade.Order.Side == Sides.Sell)
		ResetShortProtection();
	}
}

