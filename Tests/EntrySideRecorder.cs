namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

sealed class EntrySideRecorder
{
	private readonly Lock _sync = new();
	private readonly List<Entry> _entries = new();
	private decimal _position;

	/// <summary>
	/// Starts recording position entries: a fill is an entry when the position was flat before it.
	/// </summary>
	public void Attach(Strategy strategy)
		=> strategy.OwnTradeReceived += (_, trade) =>
		{
			if (trade?.Order == null || trade.Trade == null)
				return;

			using (_sync.EnterScope())
			{
				if (_position == 0m)
					_entries.Add(new(trade.Trade.ServerTime, trade.Order.Side));

				var volume = trade.Trade.Volume;
				_position += trade.Order.Side == Sides.Buy ? volume : -volume;
			}
		};

	/// <summary>
	/// Entries were taken in both directions, and enough of them to say so.
	/// </summary>
	public void AssertSupportsBothSides()
	{
		var sides = Snapshot();

		Assert.IsTrue(sides.Length >= 5, $"Too few entries to assess side support: {Format(sides)}.");
		Assert.IsTrue(sides.Contains(Sides.Buy), $"No Buy entry was observed: {Format(sides)}.");
		Assert.IsTrue(sides.Contains(Sides.Sell), $"No Sell entry was observed: {Format(sides)}.");
	}

	/// <summary>
	/// Two runs entered the same way. The reference run must have entered at all.
	/// </summary>
	public void AssertSameAs(EntrySideRecorder expected)
	{
		var expectedSides = expected.Snapshot();
		var actualSides = Snapshot();

		Assert.IsTrue(expectedSides.Length > 0, "The reference strategy produced no entries.");
		Assert.IsTrue(expectedSides.SequenceEqual(actualSides), $"Entry sides differ. Expected: {Format(expectedSides)}. Actual: {Format(actualSides)}.");
	}

	/// <summary>
	/// Two runs entered differently. Both runs must have entered at all.
	/// </summary>
	public void AssertDiffersFrom(EntrySideRecorder other)
	{
		var firstSides = Snapshot();
		var secondSides = other.Snapshot();

		Assert.IsTrue(firstSides.Length > 0 && secondSides.Length > 0, "Both strategy runs must produce entries.");
		Assert.IsFalse(firstSides.SequenceEqual(secondSides), $"Different random seeds produced the same entries: {Format(firstSides)}.");
	}

	/// <summary>
	/// No calendar day carries more entries than allowed.
	/// </summary>
	public void AssertMaximumEntriesPerDay(int maximum)
	{
		Entry[] entries;
		using (_sync.EnterScope())
			entries = _entries.ToArray();

		Assert.IsTrue(entries.Length > 0, "No position entries were filled.");

		var violations = entries
			.GroupBy(entry => entry.Time.Date)
			.Where(group => group.Count() > maximum)
			.Select(group => $"{group.Key:yyyy-MM-dd}: {group.Count()}")
			.ToArray();

		Assert.AreEqual(0, violations.Length, $"Daily entry limit {maximum} was exceeded: {string.Join(", ", violations)}.");
	}

	private Sides[] Snapshot()
	{
		using (_sync.EnterScope())
			return _entries.Select(entry => entry.Side).ToArray();
	}

	private static string Format(IEnumerable<Sides> sides)
		=> string.Join(", ", sides);

	private readonly record struct Entry(DateTime Time, Sides Side);
}
