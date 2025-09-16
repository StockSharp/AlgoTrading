using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reports the pending order type when our trades are executed.
/// </summary>
public class TypePendingOrderTriggeredStrategy : Strategy
{
	private readonly HashSet<long> _reportedOrders = [];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_reportedOrders.Clear();
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		if (trade is null)
			return;

		var order = trade.Order;

		if (order is null)
		{
			AddWarningLog("Received trade {0} without order reference.", trade.Trade?.Id);
			return;
		}

		var orderId = GetOrderKey(order);

		if (!_reportedOrders.Add(orderId))
			return;

		if (order.Type == OrderTypes.Limit)
		{
			var typeName = order.Side switch
			{
				Sides.Buy => "Buy Limit",
				Sides.Sell => "Sell Limit",
				_ => "Limit"
			};

			AddInfoLog("The pending order {0} is found! Type of order is {1}.", orderId, typeName);
			return;
		}

		if (order.Type == OrderTypes.Conditional)
		{
			var typeName = order.Side switch
			{
				Sides.Buy => "Buy Stop",
				Sides.Sell => "Sell Stop",
				_ => "Conditional"
			};

			AddInfoLog("The pending order {0} is found! Type of order is {1}.", orderId, typeName);
			return;
		}

		AddWarningLog("The order {0} is not pending. Type: {1}.", orderId, order.Type);
	}

	private static long GetOrderKey(Order order)
	{
		if (order is null)
			return default;

		if (order.Id != 0)
			return order.Id;

		if (order.TransactionId != 0)
			return order.TransactionId;

		return order.GetHashCode();
	}
}
