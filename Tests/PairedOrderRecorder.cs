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

	/// <summary>
	/// Starts recording every order the strategy submits, keeping the instrument it went to.
	/// </summary>
	public void Attach(Strategy strategy)
		=> strategy.OrderReceived += (_, order) =>
			_orders.TryAdd(order.TransactionId, (order.Security?.Id, order.Side, order.Volume));

	/// <summary>
	/// Both instruments were traded. A hedge that opens in the same direction as the primary leg is
	/// not balanced against it, so the pair shows up here rather than in <see cref="AssertBalanced"/>.
	/// </summary>
	public void AssertTradesBoth(Security primary, Security secondary)
	{
		var orders = _orders.Values.ToArray();

		Assert.IsTrue(
			orders.Any(order => order.securityId == primary.Id),
			$"No orders were submitted for {primary.Id}. Observed: {Format(orders)}.");
		Assert.IsTrue(
			orders.Any(order => order.securityId == secondary.Id),
			$"No orders were submitted for {secondary.Id}, so only one leg of the pair is traded. Observed: {Format(orders)}.");
	}

	private static string Format((string securityId, Sides side, decimal volume)[] orders)
		=> orders.Length == 0
			? "nothing"
			: string.Join(", ", orders.Select(o => $"{o.securityId} {o.side} {o.volume}").Distinct());

	/// <summary>
	/// Buys on one instrument are offset by equal sells on the other, both ways round.
	/// </summary>
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
