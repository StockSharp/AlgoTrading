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
/// Port of the MetaTrader expert advisor Grid_Template.
/// Places a symmetric grid of stop orders around the current price and rebuilds it once all orders are closed.
/// Supports optional money management and pending order expiration similar to the original template.
/// </summary>
public class GridTemplateStrategy : Strategy
{
	private readonly StrategyParam<string> _orderComment;
	private readonly StrategyParam<decimal> _staticVolume;
	private readonly StrategyParam<bool> _useMoneyManagement;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _priceDistancePips;
	private readonly StrategyParam<decimal> _gridStepPips;
	private readonly StrategyParam<int> _gridOrders;
	private readonly StrategyParam<int> _pendingExpirationHours;

	private readonly List<Order> _entryOrders = new();
	private readonly List<Order> _protectionOrders = new();

	private decimal _bestBid;
	private decimal _bestAsk;
	private decimal _pipSize;
	private DateTimeOffset? _gridExpiration;

	private decimal _stopOffsetCache;
	private decimal _takeOffsetCache;

	/// <summary>
	/// Text assigned to every order registered by the strategy.
	/// </summary>
	public string OrderComment
	{
		get => _orderComment.Value;
		set => _orderComment.Value = value;
	}

	/// <summary>
	/// Fixed order volume when money management is disabled.
	/// </summary>
	public decimal StaticVolume
	{
		get => _staticVolume.Value;
		set => _staticVolume.Value = value;
	}

	/// <summary>
	/// Enables the simplified money management routine based on account free margin.
	/// </summary>
	public bool UseMoneyManagement
	{
		get => _useMoneyManagement.Value;
		set => _useMoneyManagement.Value = value;
	}

	/// <summary>
	/// Risk percentage used by the money management formula.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Distance from the market price to the first pending order, in pips.
	/// </summary>
	public decimal PriceDistancePips
	{
		get => _priceDistancePips.Value;
		set => _priceDistancePips.Value = value;
	}

	/// <summary>
	/// Step between consecutive grid levels in pips.
	/// </summary>
	public decimal GridStepPips
	{
		get => _gridStepPips.Value;
		set => _gridStepPips.Value = value;
	}

	/// <summary>
	/// Number of pending orders placed on each side of the price.
	/// </summary>
	public int GridOrders
	{
		get => _gridOrders.Value;
		set => _gridOrders.Value = value;
	}

	/// <summary>
	/// Lifetime of the pending grid in hours. Zero disables automatic expiration.
	/// </summary>
	public int PendingExpirationHours
	{
		get => _pendingExpirationHours.Value;
		set => _pendingExpirationHours.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public GridTemplateStrategy()
	{
		_orderComment = Param(nameof(OrderComment), "Grid_Template")
			.SetDisplay("Order Comment", "Comment assigned to every order", "Orders");

		_staticVolume = Param(nameof(StaticVolume), 0.01m)
			.SetDisplay("Lot Size", "Fixed lot size when money management is disabled", "Money Management")
			.SetGreaterThanZero();

		_useMoneyManagement = Param(nameof(UseMoneyManagement), false)
			.SetDisplay("Use Money Management", "Toggle the money management routine", "Money Management");

		_riskPercent = Param(nameof(RiskPercent), 2m)
			.SetDisplay("Risk %", "Risk percentage applied to free margin", "Money Management")
			.SetGreaterThanOrEqualZero();

		_takeProfitPips = Param(nameof(TakeProfitPips), 20m)
			.SetDisplay("Take Profit (pips)", "Take-profit distance for each grid order", "Orders")
			.SetGreaterThanOrEqualZero();

		_stopLossPips = Param(nameof(StopLossPips), 30m)
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance for each grid order", "Orders")
			.SetGreaterThanOrEqualZero();

		_priceDistancePips = Param(nameof(PriceDistancePips), 15m)
			.SetDisplay("Initial Offset (pips)", "Distance from price to the first grid order", "Orders")
			.SetGreaterThanOrEqualZero();

		_gridStepPips = Param(nameof(GridStepPips), 10m)
			.SetDisplay("Grid Step (pips)", "Spacing between consecutive grid levels", "Orders")
			.SetGreaterThanOrEqualZero();

		_gridOrders = Param(nameof(GridOrders), 2)
			.SetDisplay("Orders Per Side", "Number of pending orders on each side", "Orders")
			.SetGreaterThanOrEqualZero();

		_pendingExpirationHours = Param(nameof(PendingExpirationHours), 4)
			.SetDisplay("Pending Expiration (hours)", "Time until the grid is cancelled", "Orders")
			.SetGreaterThanOrEqualZero();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, DataType.Level1);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBid = 0m;
		_bestAsk = 0m;
		_pipSize = 0m;
		_gridExpiration = null;
		_stopOffsetCache = 0m;
		_takeOffsetCache = 0m;

		_entryOrders.Clear();
		_protectionOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();

		StartProtection();
	}

	private void ProcessLevel1(Level1ChangeMessage level1)
	{
		if (level1.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bid) && bid is decimal bidPrice)
			_bestBid = bidPrice;

		if (level1.Changes.TryGetValue(Level1Fields.BestAskPrice, out var ask) && ask is decimal askPrice)
			_bestAsk = askPrice;

		var time = level1.ServerTime != default ? level1.ServerTime : CurrentTime;

		if (_gridExpiration.HasValue && time >= _gridExpiration.Value)
		{
			CancelEntryOrders();
			_gridExpiration = null;
		}

		if (Position != 0m)
			return;

		if (HasActiveOrders(_entryOrders) || HasActiveOrders(_protectionOrders))
			return;

		TryPlaceGrid(time);
	}

	private void TryPlaceGrid(DateTimeOffset time)
	{
		if (_bestBid <= 0m || _bestAsk <= 0m)
			return;

		if (GridOrders <= 0)
			return;

		var volume = CalculateOrderVolume();
		if (volume <= 0m)
			return;

		var baseOffset = ConvertPipsToPrice(PriceDistancePips);
		var stepOffset = ConvertPipsToPrice(GridStepPips);
		var stopOffset = ConvertPipsToPrice(StopLossPips);
		var takeOffset = ConvertPipsToPrice(TakeProfitPips);

		for (var index = 0; index < GridOrders; index++)
		{
			var additional = index * stepOffset;

			var buyPrice = NormalizePrice(_bestAsk + baseOffset + additional);
			if (buyPrice > 0m)
			{
				var buyOrder = BuyStop(volume, buyPrice);
				if (buyOrder != null)
				{
					buyOrder.Comment = OrderComment;
					_entryOrders.Add(buyOrder);
				}
			}

			var sellPrice = NormalizePrice(_bestBid - baseOffset - additional);
			if (sellPrice > 0m)
			{
				var sellOrder = SellStop(volume, sellPrice);
				if (sellOrder != null)
				{
					sellOrder.Comment = OrderComment;
					_entryOrders.Add(sellOrder);
				}
			}
		}

		if (_entryOrders.Count == 0)
			return;

		_gridExpiration = PendingExpirationHours > 0
			? time + TimeSpan.FromHours(PendingExpirationHours)
			: null;

		_stopOffsetCache = stopOffset;
		_takeOffsetCache = takeOffset;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		var order = trade.Order;
		if (order == null)
			return;

		if (!_entryOrders.Contains(order))
			return;

		var volume = trade.Trade.Volume ?? 0m;
		if (volume <= 0m)
			return;

		RegisterProtectionOrders(order, trade.Trade.Price, volume);
	}

	/// <inheritdoc />
	protected override void OnOrderReceived(Order order)
	{
		base.OnOrderReceived(order);

		if (order == null)
			return;

		if (_entryOrders.Remove(order) && order.State is OrderStates.Failed or OrderStates.Canceled)
			return;

		if (_protectionOrders.Remove(order) && order.State is OrderStates.Failed or OrderStates.Canceled or OrderStates.Done)
			return;

		if (_entryOrders.Count == 0 && Position == 0m && !HasActiveOrders(_protectionOrders))
			_gridExpiration = null;
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position != 0m)
			return;

		CancelProtectionOrders();
	}

	private void RegisterProtectionOrders(Order entryOrder, decimal entryPrice, decimal volume)
	{
		var stopOffset = _stopOffsetCache;
		var takeOffset = _takeOffsetCache;

		if (stopOffset <= 0m && takeOffset <= 0m)
			return;

		var side = entryOrder.Side;

		if (stopOffset > 0m)
		{
			var stopPrice = side == Sides.Buy
				? NormalizePrice(entryPrice - stopOffset)
				: NormalizePrice(entryPrice + stopOffset);

			if (stopPrice > 0m)
			{
				var stopOrder = side == Sides.Buy
					? SellStop(volume, stopPrice)
					: BuyStop(volume, stopPrice);

				if (stopOrder != null)
				{
					stopOrder.Comment = OrderComment;
					_protectionOrders.Add(stopOrder);
				}
			}
		}

		if (takeOffset > 0m)
		{
			var takePrice = side == Sides.Buy
				? NormalizePrice(entryPrice + takeOffset)
				: NormalizePrice(entryPrice - takeOffset);

			if (takePrice > 0m)
			{
				var takeOrder = side == Sides.Buy
					? SellLimit(volume, takePrice)
					: BuyLimit(volume, takePrice);

				if (takeOrder != null)
				{
					takeOrder.Comment = OrderComment;
					_protectionOrders.Add(takeOrder);
				}
			}
		}
	}

	private void CancelEntryOrders()
	{
		foreach (var order in _entryOrders.ToArray())
		{
			if (order.State is OrderStates.Pending or OrderStates.Active)
				CancelOrder(order);
		}
	}

	private void CancelProtectionOrders()
	{
		foreach (var order in _protectionOrders.ToArray())
		{
			if (order.State is OrderStates.Pending or OrderStates.Active)
				CancelOrder(order);
		}

		_protectionOrders.Clear();
	}

	private bool HasActiveOrders(List<Order> orders)
	{
		foreach (var order in orders)
		{
			if (order.State is OrderStates.Pending or OrderStates.Active)
				return true;
		}

		return false;
	}

	private decimal CalculateOrderVolume()
	{
		var volume = StaticVolume;

		if (!UseMoneyManagement)
			return NormalizeVolume(volume);

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (portfolioValue <= 0m)
			return NormalizeVolume(volume);

		var step = Security?.VolumeStep ?? 0m;
		var min = Security?.VolumeMin ?? (step > 0m ? step : 0m);
		var max = Security?.VolumeMax;

		var raw = portfolioValue * RiskPercent / 100000m;
		var adjusted = step > 0m
			? Math.Round(raw / step, MidpointRounding.AwayFromZero) * step
			: raw;

		if (min > 0m && adjusted < min)
			adjusted = min;

		if (max.HasValue && adjusted > max.Value)
			adjusted = max.Value.Value;

		return NormalizeVolume(adjusted);
	}

	private decimal NormalizeVolume(decimal volume)
	{
		if (Security == null)
			return volume;

		var step = Security.VolumeStep ?? 0m;
		if (step <= 0m)
			return volume;

		var min = Security.VolumeMin ?? step;
		var max = Security.VolumeMax;

		var adjusted = Math.Floor(volume / step) * step;
		if (adjusted < min)
			return 0m;

		if (max.HasValue && adjusted > max.Value)
			adjusted = max.Value;

		return adjusted;
	}

	private decimal NormalizePrice(decimal price)
	{
		if (Security == null)
			return price;

		var step = Security.PriceStep ?? 0m;
		if (step <= 0m)
			return price;

		return Math.Round(price / step, MidpointRounding.AwayFromZero) * step;
	}

	private decimal ConvertPipsToPrice(decimal pips)
	{
		if (pips <= 0m)
			return 0m;

		if (_pipSize <= 0m)
			_pipSize = CalculatePipSize();

		return pips * _pipSize;
	}

	private decimal CalculatePipSize()
	{
		if (Security is null)
			return 0.0001m;

		var step = Security.PriceStep ?? 0.0001m;
		var decimals = Security.Decimals;
		var multiplier = (decimals == 3 || decimals == 5) ? 10m : 1m;
		return step * multiplier;
	}
}

