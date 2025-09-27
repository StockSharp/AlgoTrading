namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that replicates the MetaTrader "CloseAll" utility for managing existing orders.
/// The strategy performs the configured bulk close action immediately when it starts.
/// </summary>
public enum CloseAllMode
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
	private readonly StrategyParam<CloseAllMode> _mode;
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
	public CloseAllMode Mode
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

		_mode = Param(nameof(Mode), CloseAllMode.CloseAll)
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
			case CloseAllMode.CloseAll:
			case CloseAllMode.CloseBuy:
			case CloseAllMode.CloseSell:
			case CloseAllMode.CloseCurrency:
			case CloseAllMode.CloseMagic:
			case CloseAllMode.CloseTicket:
			case CloseAllMode.CloseAllAndPending:
			case CloseAllMode.CloseAllAndPendingByMagic:
				ClosePositions();
				break;
		}

		switch (mode)
		{
			case CloseAllMode.ClosePending:
			case CloseAllMode.ClosePendingByMagic:
			case CloseAllMode.ClosePendingByMagicCurrency:
			case CloseAllMode.CloseAllAndPending:
			case CloseAllMode.CloseAllAndPendingByMagic:
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
			case CloseAllMode.CloseAll:
			case CloseAllMode.CloseAllAndPending:
			return true;

			case CloseAllMode.CloseBuy:
			return position.CurrentValue > 0m;

			case CloseAllMode.CloseSell:
			return position.CurrentValue < 0m;

			case CloseAllMode.CloseCurrency:
			return MatchesCurrency(position.Security, true);

			case CloseAllMode.CloseMagic:
			case CloseAllMode.CloseAllAndPendingByMagic:
			return MatchesMagicOrTicket(position);

			case CloseAllMode.CloseTicket:
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
			case CloseAllMode.ClosePending:
			case CloseAllMode.CloseAllAndPending:
			return true;

			case CloseAllMode.ClosePendingByMagic:
			case CloseAllMode.CloseAllAndPendingByMagic:
			return MatchesMagicOrTicket(order);

			case CloseAllMode.ClosePendingByMagicCurrency:
			return MatchesMagicOrTicket(order) && MatchesCurrency(order.Security, true);

			default:
			return false;
		}
	}

	private bool MatchesComment(string comment)
	{
		var filter = (OrderComment ?? string.Empty).Trim();

		if (string.IsNullOrEmpty(filter))
			return true;

		if (string.IsNullOrEmpty(comment))
			return false;

		return comment.Trim().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private bool MatchesCurrency(Security security, bool requireCurrency)
	{
		var filter = (CurrencyId ?? string.Empty).Trim();

		if (string.IsNullOrEmpty(filter))
		{
			if (!requireCurrency)
				return true;

			var current = Security;
			return current == null || Equals(security, current);
		}

		return security != null && string.Equals(security.Id, filter, StringComparison.OrdinalIgnoreCase);
	}

	private bool MatchesMagicOrTicket(Position position)
	{
		return MatchesIdentifier(position.Id?.ToString()) || MatchesIdentifier(TryGetStrategyId(position));
	}

	private bool MatchesMagicOrTicket(Order order)
	{
		if (MatchesIdentifier(order.TransactionId.ToString(CultureInfo.InvariantCulture)))
			return true;

		if (order.Id != null && MatchesIdentifier(order.Id.ToString() ?? string.Empty))
			return true;

		if (!string.IsNullOrEmpty(order.UserOrderId) && MatchesIdentifier(order.UserOrderId))
			return true;

		return MatchesIdentifier(TryGetStrategyId(order));
	}

	private bool MatchesTicket(Position position)
	{
		return MatchesIdentifier(position.Id?.ToString());
	}

	private bool MatchesIdentifier(string value)
	{
		var target = MagicOrTicket;

		if (target <= 0)
			return false;

		if (string.IsNullOrEmpty(value))
			return false;

		if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
			return parsed == target;

		return string.Equals(value, target.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
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
		return position.StrategyId?.ToString();
	}

	private static string TryGetStrategyId(Order order)
	{
		return order.StrategyId?.ToString();
	}
}
