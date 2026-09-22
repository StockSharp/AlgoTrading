namespace StockSharp.Tests;

using System.Collections.Concurrent;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

sealed class PairedOrderRecorder
{
	private readonly ConcurrentDictionary<long, (string securityId, Sides side, decimal volume)> _orders = new();

	public void Attach(Strategy strategy)
		=> strategy.OrderReceived += (_, order) =>
			_orders.TryAdd(order.TransactionId, (order.Security?.Id, order.Side, order.Volume));

	public void AssertBalanced(Security primary, Security secondary)
	{
		var orders = _orders.Values.ToArray();
		var primaryOrders = orders.Where(order => order.securityId == primary.Id).ToArray();
		var secondaryOrders = orders.Where(order => order.securityId == secondary.Id).ToArray();

		Assert.IsTrue(primaryOrders.Length > 0, $"No orders were submitted for the primary security {primary.Id}.");
		Assert.IsTrue(secondaryOrders.Length > 0, $"No orders were submitted for the hedge security {secondary.Id}.");

		Assert.AreEqual(
			primaryOrders.Where(order => order.side == Sides.Buy).Sum(order => order.volume),
			secondaryOrders.Where(order => order.side == Sides.Sell).Sum(order => order.volume),
			"Primary buys were not offset by equal hedge-security sells.");
		Assert.AreEqual(
			primaryOrders.Where(order => order.side == Sides.Sell).Sum(order => order.volume),
			secondaryOrders.Where(order => order.side == Sides.Buy).Sum(order => order.volume),
			"Primary sells were not offset by equal hedge-security buys.");
	}
}
