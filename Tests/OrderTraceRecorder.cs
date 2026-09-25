namespace StockSharp.Tests;

using System;
using System.Collections.Concurrent;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

sealed class OrderTraceRecorder
{
	private readonly ConcurrentDictionary<long, OrderTraceEntry> _orders = new();
	private DateTime? _startedAt;

	/// <summary>
	/// Starts recording every order the strategy submits, in the order they arrive.
	/// </summary>
	public void Attach(Strategy strategy)
	{
		strategy.ProcessStateChanged += changed =>
		{
			if (ReferenceEquals(changed, strategy) && changed.ProcessState == ProcessStates.Started)
				_startedAt ??= strategy.CurrentTime;
		};

		strategy.OrderReceived += (_, order) =>
			_orders.TryAdd(order.TransactionId, new(strategy.CurrentTime, order.Side, order.Volume, order.Comment, order.Security?.Id, order.Type));
	}

	public void AssertAtMostOneOrderPerTimestamp()
	{
		var trace = Snapshot();
		var duplicate = trace.GroupBy(entry => entry.Time).FirstOrDefault(group => group.Count() > 1);

		Assert.IsNull(
			duplicate,
			duplicate is null ? null : $"Multiple orders were submitted at {duplicate.Key:O}. Trace: {Format(trace)}.");
	}

	public void AssertFirstTwoAreOppositeConditionalStops()
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length >= 2, $"Expected at least two orders. Trace: {Format(trace)}.");
		Assert.AreEqual(OrderTypes.Conditional, trace[0].Type, $"First order must be a conditional stop. Trace: {Format(trace)}.");
		Assert.AreEqual(OrderTypes.Conditional, trace[1].Type, $"Second order must be a conditional stop. Trace: {Format(trace)}.");
		Assert.AreNotEqual(trace[0].Side, trace[1].Side, $"Initial stop orders must be opposite sides. Trace: {Format(trace)}.");
	}

	public void AssertFirstSide(Sides expectedSide)
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length > 0, "No orders were submitted.");
		Assert.AreEqual(expectedSide, trace[0].Side, $"Unexpected first-order side. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// The first order was submitted in the expected hour of the day.
	/// </summary>
	public void AssertFirstOrderAfterStart(TimeSpan minimumDelay)
	{
		var trace = Snapshot();

		Assert.IsTrue(_startedAt is not null, "Strategy start time was not recorded.");
		Assert.IsTrue(trace.Length > 0, "No orders were submitted.");
		Assert.IsTrue(
			trace[0].Time - _startedAt.Value > minimumDelay,
			$"First order arrived after {trace[0].Time - _startedAt.Value}, expected more than {minimumDelay}. Trace: {Format(trace)}.");
	}

	public void AssertFirstOrderHour(int expectedHour)
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length > 0, "No orders were submitted.");
		Assert.AreEqual(expectedHour, trace[0].Time.Hour, $"Unexpected first-order hour. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// The first order carried the expected volume.
	/// </summary>
	public void AssertFirstVolume(decimal expectedVolume)
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length > 0, "No orders were submitted.");
		Assert.AreEqual(expectedVolume, trace[0].Volume, $"Unexpected first-order volume. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// Some order carried the expected volume, whichever one it was.
	/// </summary>
	public void AssertContainsVolume(decimal expectedVolume)
	{
		var trace = Snapshot();

		Assert.IsTrue(
			trace.Any(entry => entry.Volume == expectedVolume),
			$"No order used the expected volume {expectedVolume}. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// A reversal was submitted as one order of twice the base volume, and nothing exceeded it.
	/// </summary>
	public void AssertReverses(decimal baseVolume)
	{
		var trace = Snapshot();
		var reversalVolume = baseVolume * 2;

		Assert.IsTrue(
			trace.Any(entry => entry.Volume == reversalVolume),
			$"No reversal order with volume {reversalVolume} was submitted. Trace: {Format(trace)}.");
		Assert.IsFalse(
			trace.Any(entry => entry.Volume > reversalVolume),
			$"An order exceeded the bounded reversal volume {reversalVolume}. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// The first order carried the expected comment.
	/// </summary>
	public void AssertContainsTwoLegSignal(string expectedComment)
	{
		var trace = Snapshot();
		var group = trace
			.Where(entry => entry.Comment == expectedComment)
			.GroupBy(entry => entry.Time)
			.FirstOrDefault(entries => entries.Select(entry => entry.SecurityId).Where(id => !string.IsNullOrEmpty(id)).Distinct(StringComparer.Ordinal).Count() >= 2);

		Assert.IsNotNull(group, $"No two-leg '{expectedComment}' signal was submitted. Trace: {Format(trace)}.");
	}

	public void AssertFirstComment(string expectedComment)
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length > 0, "No orders were submitted.");
		Assert.AreEqual(expectedComment, trace[0].Comment, $"Unexpected first-order comment. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// The first order opposite to the first entry followed it within the given time.
	/// </summary>
	public void AssertFirstOppositeWithin(TimeSpan maximumDelay)
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length > 0, "No orders were submitted.");
		var first = trace[0];
		var opposite = trace.Skip(1).FirstOrDefault(entry => entry.Side != first.Side);
		Assert.AreNotEqual(default, opposite, $"No order opposite to the first entry was submitted. Trace: {Format(trace)}.");
		Assert.IsTrue(
			opposite.Time - first.Time <= maximumDelay,
			$"The first opposite order was delayed by {opposite.Time - first.Time}, exceeding {maximumDelay}. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// The first order opposite to the first entry waited at least the given time.
	/// </summary>
	public void AssertNoSameSideReentryWithinAfterFirstExit(TimeSpan minimumDelay)
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length > 1, $"Too few orders to contain an exit. Trace: {Format(trace)}.");
		var first = trace[0];
		var exitIndex = Array.FindIndex(trace, 1, entry => entry.Side != first.Side);
		Assert.IsTrue(exitIndex > 0, $"No opposite exit was submitted. Trace: {Format(trace)}.");

		var exit = trace[exitIndex];
		var reentry = trace.Skip(exitIndex + 1).FirstOrDefault(entry => entry.Side == first.Side);
		if (reentry == default)
			return;

		Assert.IsTrue(
			reentry.Time - exit.Time > minimumDelay,
			$"Same-side re-entry followed the exit after {reentry.Time - exit.Time}, expected more than {minimumDelay}. Trace: {Format(trace)}.");
	}

	public void AssertFirstOppositeAfter(TimeSpan minimumDelay)
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length > 0, "No orders were submitted.");
		var first = trace[0];
		var opposite = trace.Skip(1).FirstOrDefault(entry => entry.Side != first.Side);
		Assert.AreNotEqual(default, opposite, $"No order opposite to the first entry was submitted. Trace: {Format(trace)}.");
		Assert.IsTrue(
			opposite.Time - first.Time >= minimumDelay,
			$"The first opposite order arrived after {opposite.Time - first.Time}, before the minimum {minimumDelay}. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// A run of same-side entries was closed by one opposite order of the expected size.
	/// </summary>
	public void AssertBasketExitAfterEntries(int expectedEntryCount, decimal expectedExitVolume)
	{
		var trace = Snapshot();

		Assert.IsTrue(trace.Length > expectedEntryCount, $"Too few orders to contain a basket exit. Trace: {Format(trace)}.");

		for (var start = 0; start + expectedEntryCount < trace.Length; start++)
		{
			var side = trace[start].Side;
			if (trace.Skip(start).Take(expectedEntryCount).Any(entry => entry.Side != side))
				continue;

			var exit = trace[start + expectedEntryCount];
			if (exit.Side == side)
				continue;

			Assert.AreEqual(expectedExitVolume, exit.Volume, $"Unexpected basket-exit volume. Trace: {Format(trace)}.");
			return;
		}

		Assert.Fail($"No {expectedEntryCount}-entry basket followed by an opposite order was submitted. Trace: {Format(trace)}.");
	}

	/// <summary>
	/// Two runs submitted the same orders. The reference run must have submitted something.
	/// </summary>
	public void AssertSameAs(OrderTraceRecorder expected)
	{
		var expectedTrace = expected.Snapshot();
		var actualTrace = Snapshot();

		Assert.IsTrue(expectedTrace.Length > 0, "The reference strategy submitted no orders.");
		Assert.IsTrue(
			expectedTrace.SequenceEqual(actualTrace),
			$"Order traces differ. Expected: {Format(expectedTrace)}. Actual: {Format(actualTrace)}.");
	}

	/// <summary>
	/// Two runs submitted different orders. Both runs must have submitted something.
	/// </summary>
	public void AssertDiffersFrom(OrderTraceRecorder other, string reason)
	{
		var firstTrace = Snapshot();
		var secondTrace = other.Snapshot();

		Assert.IsTrue(firstTrace.Length > 0, "The first strategy run submitted no orders.");
		Assert.IsTrue(secondTrace.Length > 0, "The second strategy run submitted no orders.");
		Assert.IsFalse(
			firstTrace.SequenceEqual(secondTrace),
			$"{reason} First: {Format(firstTrace)}. Second: {Format(secondTrace)}.");
	}

	private OrderTraceEntry[] Snapshot()
		=> _orders.Values
			.OrderBy(entry => entry.Time)
			.ThenBy(entry => entry.Side)
			.ThenBy(entry => entry.Volume)
			.ToArray();

	private static string Format(OrderTraceEntry[] trace)
	{
		const int previewLength = 20;
		var preview = string.Join(", ", trace.Take(previewLength).Select(entry => $"{entry.Time:O} {entry.SecurityId ?? "<null>"} {entry.Type} {entry.Side} {entry.Volume} '{entry.Comment}'"));
		return trace.Length <= previewLength ? preview : $"{preview}, ... ({trace.Length} total)";
	}

	private readonly record struct OrderTraceEntry(DateTime Time, Sides Side, decimal Volume, string Comment, string SecurityId, OrderTypes Type);
}
