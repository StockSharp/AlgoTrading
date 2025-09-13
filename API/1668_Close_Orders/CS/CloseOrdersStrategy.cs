using System;
using System.Linq;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Utility strategy that closes positions and pending orders.
/// </summary>
public class CloseOrdersStrategy : Strategy
{
	private readonly StrategyParam<bool> _closeAllSecurities;
	private readonly StrategyParam<bool> _closeOpenLongOrders;
	private readonly StrategyParam<bool> _closeOpenShortOrders;
	private readonly StrategyParam<bool> _closePendingLongOrders;
	private readonly StrategyParam<bool> _closePendingShortOrders;
	private readonly StrategyParam<long> _specificOrderId;
	private readonly StrategyParam<bool> _closeOrdersWithinRange;
	private readonly StrategyParam<decimal> _closeRangeHigh;
	private readonly StrategyParam<decimal> _closeRangeLow;
	private readonly StrategyParam<bool> _enableTimeControl;
	private readonly StrategyParam<TimeSpan> _startCloseTime;
	private readonly StrategyParam<TimeSpan> _stopCloseTime;

	/// <summary>
	/// Close orders for all securities or only for attached security.
	/// </summary>
	public bool CloseAllSecurities
	{
		get => _closeAllSecurities.Value;
		set => _closeAllSecurities.Value = value;
	}

	/// <summary>
	/// Close open long positions.
	/// </summary>
	public bool CloseOpenLongOrders
	{
		get => _closeOpenLongOrders.Value;
		set => _closeOpenLongOrders.Value = value;
	}

	/// <summary>
	/// Close open short positions.
	/// </summary>
	public bool CloseOpenShortOrders
	{
		get => _closeOpenShortOrders.Value;
		set => _closeOpenShortOrders.Value = value;
	}

	/// <summary>
	/// Cancel pending buy orders.
	/// </summary>
	public bool ClosePendingLongOrders
	{
		get => _closePendingLongOrders.Value;
		set => _closePendingLongOrders.Value = value;
	}

	/// <summary>
	/// Cancel pending sell orders.
	/// </summary>
	public bool ClosePendingShortOrders
	{
		get => _closePendingShortOrders.Value;
		set => _closePendingShortOrders.Value = value;
	}

	/// <summary>
	/// Close only orders with specific transaction id. Set to 0 to ignore.
	/// </summary>
	public long SpecificOrderId
	{
		get => _specificOrderId.Value;
		set => _specificOrderId.Value = value;
	}

	/// <summary>
	/// Limit closing to orders with entry price within range.
	/// </summary>
	public bool CloseOrdersWithinRange
	{
		get => _closeOrdersWithinRange.Value;
		set => _closeOrdersWithinRange.Value = value;
	}

	/// <summary>
	/// Upper bound of allowed price range.
	/// </summary>
	public decimal CloseRangeHigh
	{
		get => _closeRangeHigh.Value;
		set => _closeRangeHigh.Value = value;
	}

	/// <summary>
	/// Lower bound of allowed price range.
	/// </summary>
	public decimal CloseRangeLow
	{
		get => _closeRangeLow.Value;
		set => _closeRangeLow.Value = value;
	}

	/// <summary>
	/// Enable time window control.
	/// </summary>
	public bool EnableTimeControl
	{
		get => _enableTimeControl.Value;
		set => _enableTimeControl.Value = value;
	}

	/// <summary>
	/// Start time of allowed closing window.
	/// </summary>
	public TimeSpan StartCloseTime
	{
		get => _startCloseTime.Value;
		set => _startCloseTime.Value = value;
	}

	/// <summary>
	/// End time of allowed closing window.
	/// </summary>
	public TimeSpan StopCloseTime
	{
		get => _stopCloseTime.Value;
		set => _stopCloseTime.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CloseOrdersStrategy()
	{
		_closeAllSecurities = Param(nameof(CloseAllSecurities), true)
			.SetDisplay("Close All Securities", "Apply closing to all securities", "General");

		_closeOpenLongOrders = Param(nameof(CloseOpenLongOrders), true)
			.SetDisplay("Close Open Long Orders", "Close existing long positions", "General");

		_closeOpenShortOrders = Param(nameof(CloseOpenShortOrders), true)
			.SetDisplay("Close Open Short Orders", "Close existing short positions", "General");

		_closePendingLongOrders = Param(nameof(ClosePendingLongOrders), true)
			.SetDisplay("Close Pending Long Orders", "Cancel pending buy orders", "General");

		_closePendingShortOrders = Param(nameof(ClosePendingShortOrders), true)
			.SetDisplay("Close Pending Short Orders", "Cancel pending sell orders", "General");

		_specificOrderId = Param(nameof(SpecificOrderId), 0L)
			.SetDisplay("Specific Order Id", "Close only orders with this transaction id", "Filters");

		_closeOrdersWithinRange = Param(nameof(CloseOrdersWithinRange), false)
			.SetDisplay("Limit By Price Range", "Close orders only within specified price range", "Filters");

		_closeRangeHigh = Param(nameof(CloseRangeHigh), 0m)
			.SetDisplay("Close Range High", "Upper price boundary", "Filters");

		_closeRangeLow = Param(nameof(CloseRangeLow), 0m)
			.SetDisplay("Close Range Low", "Lower price boundary", "Filters");

		_enableTimeControl = Param(nameof(EnableTimeControl), false)
			.SetDisplay("Enable Time Control", "Restrict closing to specified time window", "Time");

		_startCloseTime = Param(nameof(StartCloseTime), TimeSpan.FromHours(2))
			.SetDisplay("Start Close Time", "Start of allowed closing window", "Time");

		_stopCloseTime = Param(nameof(StopCloseTime), TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(30)))
			.SetDisplay("Stop Close Time", "End of allowed closing window", "Time");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (EnableTimeControl)
		{
			var current = time.TimeOfDay;

			if (StartCloseTime < StopCloseTime)
			{
				if (current < StartCloseTime || current >= StopCloseTime)
				{
					Stop();
					return;
				}
			}
			else if (StartCloseTime > StopCloseTime)
			{
				if (current < StartCloseTime && current >= StopCloseTime)
				{
					Stop();
					return;
				}
			}
			else
			{
				Stop();
				return;
			}
		}

		CloseOrders();

		Stop();
	}

	private void CloseOrders()
	{
		if (CloseAllSecurities)
		{
			foreach (var position in Portfolio.Positions.ToArray())
				ProcessPosition(position);

			foreach (var order in Orders.Where(o => o.State == OrderStates.Active).ToArray())
				ProcessOrder(order);
		}
		else
		{
			var position = Portfolio.Positions.FirstOrDefault(p => p.Security == Security);
			if (position != null)
				ProcessPosition(position);

			foreach (var order in Orders.Where(o => o.Security == Security && o.State == OrderStates.Active).ToArray())
				ProcessOrder(order);
		}
	}

	private void ProcessPosition(Position position)
	{
		if (!IsInRange(position.AveragePrice))
			return;

		if (position.CurrentValue > 0 && CloseOpenLongOrders)
			ClosePosition(position.Security);
		else if (position.CurrentValue < 0 && CloseOpenShortOrders)
			ClosePosition(position.Security);
	}

	private void ProcessOrder(Order order)
	{
		if (SpecificOrderId != 0 && order.TransactionId != SpecificOrderId)
			return;

		if (!IsInRange(order.Price))
			return;

		if (order.Side == Sides.Buy && ClosePendingLongOrders)
			CancelOrder(order);
		else if (order.Side == Sides.Sell && ClosePendingShortOrders)
			CancelOrder(order);
	}

	private bool IsInRange(decimal? price)
	{
		if (!CloseOrdersWithinRange)
			return true;

		if (CloseRangeHigh <= 0m || CloseRangeLow <= 0m || price is null)
			return true;

		return price < CloseRangeHigh && price > CloseRangeLow;
	}
}
