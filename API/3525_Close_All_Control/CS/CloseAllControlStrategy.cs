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

using System.Globalization;
using System.Reflection;

/// <summary>
/// Strategy that replicates the MetaTrader "CloseAll" utility for managing existing orders.
/// The strategy performs the configured bulk close action immediately when it starts.
/// </summary>
public enum CloseAllModes
{
	/// <summary>
	/// Close all open positions that match the comment filter.
	/// </summary>
	CloseAll,

	/// <summary>
	/// Close only long positions that match the comment filter.
	/// </summary>
	CloseBuy,

	/// <summary>
	/// Close only short positions that match the comment filter.
	/// </summary>
	CloseSell,

	/// <summary>
	/// Close positions for the configured symbol.
	/// </summary>
	CloseCurrency,

	/// <summary>
	/// Close positions whose identifier matches the magic number.
	/// </summary>
	CloseMagic,

	/// <summary>
	/// Close a single position identified by its ticket number.
	/// </summary>
	CloseTicket,

	/// <summary>
	/// Cancel pending orders that match the magic number.
	/// </summary>
	ClosePendingByMagic,

	/// <summary>
	/// Cancel pending orders that match both the magic number and the target symbol.
	/// </summary>
	ClosePendingByMagicCurrency,

	/// <summary>
	/// Close positions and pending orders that match the magic number.
	/// </summary>
	CloseAllAndPendingByMagic,

	/// <summary>
	/// Cancel all pending orders that match the comment filter.
	/// </summary>
	ClosePending,

	/// <summary>
	/// Close all positions and cancel all pending orders that match the comment filter.
	/// </summary>
	CloseAllAndPending,
}

/// <summary>
/// StockSharp conversion of the MQL "CloseAll" utility that bulk closes positions and pending orders.
/// </summary>
public class CloseAllControlStrategy : Strategy
{
	private readonly StrategyParam<string> _orderComment;
	private readonly StrategyParam<CloseAllModes> _mode;
	private readonly StrategyParam<string> _currencyId;
	private readonly StrategyParam<long> _magicOrTicket;

	/// <summary>
	/// Gets or sets the substring that must be present inside the order comment.
	/// Leave empty to process every order regardless of the comment.
	/// </summary>
	public string OrderComment
	{
		get => _orderComment.Value;
		set => _orderComment.Value = value;
	}

	/// <summary>
	/// Gets or sets which close scenario must be executed when the strategy starts.
	/// </summary>
	public CloseAllModes Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Gets or sets the security identifier used by the currency specific modes.
	/// </summary>
	public string CurrencyId
	{
		get => _currencyId.Value;
		set => _currencyId.Value = value;
	}

	/// <summary>
	/// Gets or sets the magic number or ticket number used by the dedicated modes.
	/// </summary>
	public long MagicOrTicket
	{
		get => _magicOrTicket.Value;
		set => _magicOrTicket.Value = value;
	}

	/// <summary>
	/// Initializes the strategy parameters.
	/// </summary>
	public CloseAllControlStrategy()
	{
		_orderComment = Param(nameof(OrderComment), "Bonnitta EA")
			.SetDisplay("Order Comment", "Substring that must be present in the order comment to be processed.", "Filters");

		_mode = Param(nameof(Mode), CloseAllModes.CloseAll)
			.SetDisplay("Mode", "Bulk close scenario executed once the strategy starts.", "Execution");

		_currencyId = Param(nameof(CurrencyId), string.Empty)
			.SetDisplay("Currency Id", "Optional security identifier used by currency specific modes.", "Filters");

		_magicOrTicket = Param(nameof(MagicOrTicket), 1L)
			.SetDisplay("Magic / Ticket", "Identifier matched against order or position ids.", "Filters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio must be assigned before starting the strategy.");

		StartProtection();

		ProcessCloseActions();

		Stop();
	}

	private void ProcessCloseActions()
	{
		var mode = Mode;

		switch (mode)
		{
			case CloseAllModes.CloseAll:
			case CloseAllModes.CloseBuy:
			case CloseAllModes.CloseSell:
			case CloseAllModes.CloseCurrency:
			case CloseAllModes.CloseMagic:
			case CloseAllModes.CloseTicket:
			case CloseAllModes.CloseAllAndPending:
			case CloseAllModes.CloseAllAndPendingByMagic:
				ClosePositions();
				break;
		}

		switch (mode)
		{
			case CloseAllModes.ClosePending:
			case CloseAllModes.ClosePendingByMagic:
			case CloseAllModes.ClosePendingByMagicCurrency:
			case CloseAllModes.CloseAllAndPending:
			case CloseAllModes.CloseAllAndPendingByMagic:
				CancelPendingOrders();
				break;
		}
	}

	private void ClosePositions()
	{
		var portfolio = Portfolio;
		if (portfolio == null)
			return;

		var positionsToClose = new List<Position>();

		foreach (var position in portfolio.Positions)
		{
			if (ShouldClosePosition(position))
				positionsToClose.Add(position);
		}

		foreach (var position in positionsToClose)
		{
			var volume = position.CurrentValue;
			var security = position.Security;

			if (security == null || volume == 0m)
				continue;

			if (volume > 0m)
				SellMarket(volume, security);
			else
				BuyMarket(-volume, security);
		}
	}

	private void CancelPendingOrders()
	{
		var ordersToCancel = new List<Order>();

		foreach (var order in ActiveOrders)
		{
			if (!IsPendingOrder(order))
				continue;

			if (ShouldCancelOrder(order))
				ordersToCancel.Add(order);
		}

		foreach (var order in ordersToCancel)
		CancelOrder(order);
	}

	private bool ShouldClosePosition(Position position)
	{
		if (!MatchesComment(TryGetPositionComment(position)))
			return false;

		var mode = Mode;

		switch (mode)
		{
			case CloseAllModes.CloseAll:
			case CloseAllModes.CloseAllAndPending:
			return true;

			case CloseAllModes.CloseBuy:
			return position.CurrentValue > 0m;

			case CloseAllModes.CloseSell:
			return position.CurrentValue < 0m;

			case CloseAllModes.CloseCurrency:
			return MatchesCurrency(position.Security, true);

			case CloseAllModes.CloseMagic:
			case CloseAllModes.CloseAllAndPendingByMagic:
			return MatchesMagicOrTicket(position);

			case CloseAllModes.CloseTicket:
			return MatchesTicket(position);

			default:
			return false;
		}
	}

	private bool ShouldCancelOrder(Order order)
	{
		if (!MatchesComment(order.Comment))
			return false;

		var mode = Mode;

		switch (mode)
		{
			case CloseAllModes.ClosePending:
			case CloseAllModes.CloseAllAndPending:
			return true;

			case CloseAllModes.ClosePendingByMagic:
			case CloseAllModes.CloseAllAndPendingByMagic:
			return MatchesMagicOrTicket(order);

			case CloseAllModes.ClosePendingByMagicCurrency:
			return MatchesMagicOrTicket(order) && MatchesCurrency(order.Security, true);

			default:
			return false;
		}
	}

	private bool MatchesComment(string comment)
	{
		var filter = (OrderComment ?? string.Empty).Trim();

		if (filter.IsEmpty())
			return true;

		if (comment.IsEmpty())
			return false;

		return comment.Trim().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private bool MatchesCurrency(Security security, bool requireCurrency)
	{
		var filter = (CurrencyId ?? string.Empty).Trim();

		if (filter.IsEmpty())
		{
			if (!requireCurrency)
				return true;

			var current = Security;
			return current == null || Equals(security, current);
		}

		return security != null && security.Id.EqualsIgnoreCase(filter);
	}

	private bool MatchesMagicOrTicket(Position position)
	{
		return MatchesIdentifier(position.Id) || MatchesIdentifier(TryGetStrategyId(position));
	}

	private bool MatchesMagicOrTicket(Order order)
	{
		if (MatchesIdentifier(order.TransactionId.ToString(CultureInfo.InvariantCulture)))
			return true;

		if (order.Id != null && MatchesIdentifier(order.Id.ToString() ?? string.Empty))
			return true;

		if (!order.UserOrderId.IsEmpty() && MatchesIdentifier(order.UserOrderId))
			return true;

		return MatchesIdentifier(TryGetStrategyId(order));
	}

	private bool MatchesTicket(Position position)
	{
		return MatchesIdentifier(position.Id);
	}

	private bool MatchesIdentifier(string value)
	{
		var target = MagicOrTicket;

		if (target <= 0)
			return false;

		if (value.IsEmpty())
			return false;

		if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
			return parsed == target;

		return value.EqualsIgnoreCase(target.ToString(CultureInfo.InvariantCulture));
	}

	private static bool IsPendingOrder(Order order)
	{
		switch (order.Type)
		{
			case OrderTypes.Market:
			return false;
			default:
			return true;
		}
	}

	private static string TryGetStrategyId(Position position)
	{
		return position.StrategyId;
	}

	private static string TryGetStrategyId(Order order)
	{
		return order.StrategyId;
	}
}

