using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy that closes orders and positions at a specified local time.
/// </summary>
public class CloseAtTimeStrategy : Strategy
{
	private readonly StrategyParam<bool> _closeAll;
	private readonly StrategyParam<bool> _closeBySymbol;
	private readonly StrategyParam<bool> _closeByMagicNumber;
	private readonly StrategyParam<bool> _closeByTicket;
	private readonly StrategyParam<bool> _closePending;
	private readonly StrategyParam<bool> _closeMarket;
	private readonly StrategyParam<DateTimeOffset> _timeToClose;
	private readonly StrategyParam<string> _symbolToClose;
	private readonly StrategyParam<string> _magicNumber;
	private readonly StrategyParam<string> _ticketNumber;

	/// <summary>
	/// Close all active orders and positions.
	/// </summary>
	public bool CloseAll
	{
		get => _closeAll.Value;
		set => _closeAll.Value = value;
	}

	/// <summary>
	/// Close by security code.
	/// </summary>
	public bool CloseBySymbol
	{
		get => _closeBySymbol.Value;
		set => _closeBySymbol.Value = value;
	}

	/// <summary>
	/// Close orders by magic number (UserOrderId).
	/// </summary>
	public bool CloseByMagicNumber
	{
		get => _closeByMagicNumber.Value;
		set => _closeByMagicNumber.Value = value;
	}

	/// <summary>
	/// Close order by ticket number (Id).
	/// </summary>
	public bool CloseByTicket
	{
		get => _closeByTicket.Value;
		set => _closeByTicket.Value = value;
	}

	/// <summary>
	/// Cancel pending orders.
	/// </summary>
	public bool ClosePendingOrders
	{
		get => _closePending.Value;
		set => _closePending.Value = value;
	}

	/// <summary>
	/// Close open positions with market orders.
	/// </summary>
	public bool CloseMarketOrders
	{
		get => _closeMarket.Value;
		set => _closeMarket.Value = value;
	}

	/// <summary>
	/// Local time to start closing procedure.
	/// </summary>
	public DateTimeOffset TimeToClose
	{
		get => _timeToClose.Value;
		set => _timeToClose.Value = value;
	}

	/// <summary>
	/// Security code used when <see cref="CloseBySymbol"/> is enabled.
	/// </summary>
	public string SymbolToClose
	{
		get => _symbolToClose.Value;
		set => _symbolToClose.Value = value;
	}

	/// <summary>
	/// Target magic number.
	/// </summary>
	public string MagicNumber
	{
		get => _magicNumber.Value;
		set => _magicNumber.Value = value;
	}

	/// <summary>
	/// Target ticket number.
	/// </summary>
	public string TicketNumber
	{
		get => _ticketNumber.Value;
		set => _ticketNumber.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CloseAtTimeStrategy()
	{
		_closeAll = Param(nameof(CloseAll), false)
			.SetDisplay("Close All", "Close all active orders and positions", "Close Settings");

		_closeBySymbol = Param(nameof(CloseBySymbol), false)
			.SetDisplay("Close by Symbol", "Close only items for specified symbol", "Close Settings");

		_closeByMagicNumber = Param(nameof(CloseByMagicNumber), false)
			.SetDisplay("Close by Magic", "Close orders with specific magic number", "Close Settings");

		_closeByTicket = Param(nameof(CloseByTicket), false)
			.SetDisplay("Close by Ticket", "Close order with specific ticket id", "Close Settings");

		_closePending = Param(nameof(ClosePendingOrders), false)
			.SetDisplay("Close Pending", "Cancel pending orders", "Close Settings");

		_closeMarket = Param(nameof(CloseMarketOrders), false)
			.SetDisplay("Close Market", "Close market positions", "Close Settings");

		_timeToClose = Param(nameof(TimeToClose), DateTimeOffset.Now)
			.SetDisplay("Time To Close", "Local time when closing starts", "Close Parameters");

		_symbolToClose = Param(nameof(SymbolToClose), string.Empty)
			.SetDisplay("Symbol", "Symbol to close", "Close Parameters");

		_magicNumber = Param(nameof(MagicNumber), string.Empty)
			.SetDisplay("Magic Number", "Order.UserOrderId to match", "Close Parameters");

		_ticketNumber = Param(nameof(TicketNumber), string.Empty)
			.SetDisplay("Ticket Number", "Order identifier to match", "Close Parameters");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield break;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var delay = TimeToClose - time;
		if (delay < TimeSpan.Zero)
			delay = TimeSpan.Zero;

		Task.Run(async () =>
		{
			await Task.Delay(delay);
			CloseAllActions();
		});
	}

	private void CloseAllActions()
	{
		var orders = new List<Order>(ActiveOrders);
		foreach (var order in orders)
		{
			if (!ShouldClose(order))
				continue;

			if (ClosePendingOrders && order.Type != OrderTypes.Market)
				CancelOrder(order);
		}

		if (!CloseMarketOrders)
			return;

		var positions = new List<Position>(Positions);
		foreach (var position in positions)
		{
			if (!ShouldClose(position))
				continue;

			ClosePosition(position.Security);
		}
	}

	private bool ShouldClose(Order order)
	{
		if (CloseAll)
			return true;

		if (CloseBySymbol && order.Security?.Code == SymbolToClose)
			return true;

		if (CloseByTicket && order.Id?.ToString() == TicketNumber)
			return true;

		if (CloseByMagicNumber && order.UserOrderId == MagicNumber)
			return true;

		return false;
	}

	private bool ShouldClose(Position position)
	{
		if (CloseAll)
			return true;

		if (CloseBySymbol && position.Security?.Code == SymbolToClose)
			return true;

		return false;
	}
}
