namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Point 1 / part 02: independent archive oracle for the published price/volume breakout.
/// Both subclasses execute this test, one from C# and one from Python source.
/// </summary>
public abstract partial class StrategyTests
{
	private const string Point1Part02Breakout = "1186_Price_and_Volume_Breakout_Buy";

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow("Long", false)]
	[DataRow("Short", false)]
	[DataRow("Both", false)]
	[DataRow("Long", true)]
	[DataRow("Short", true)]
	public async Task S1186_PriorExtremaSmaDirectionAndFiveOppositeCloses(string direction, bool secondary)
	{
		const int priceWindow = 3;
		const int volumeWindow = 3;
		const int smaWindow = 5;
		var bars = new List<(decimal high, decimal low, decimal close, decimal volume)>();
		var actualPosition = 0m;
		var oppositeCloses = 0;
		var expectedEntries = 0;
		var expectedExits = 0;
		var fills = 0;
		var qualifiedLongs = 0;
		var qualifiedShorts = 0;
		var orders = 0;
		var rejects = 0;
		var interruptions = 0;
		var pending = (Order)null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var errors = new List<string>();

		await Replay(Point1Part02Breakout, (strategy, other) =>
		{
			// Assert unmodified published defaults before using an explicitly reachable
			// five-minute archive profile; a month cannot warm up 200 daily bars.
			AreEqual(60, strategy.Parameters["PriceBreakoutPeriod"].Value);
			AreEqual(60, strategy.Parameters["VolumeBreakoutPeriod"].Value);
			AreEqual(200, strategy.Parameters["TrendlineLength"].Value);
			AreEqual("Long", strategy.Parameters["OrderDirection"].Value);
			AreEqual(TimeSpan.FromDays(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			if (secondary)
				strategy.Security = other;
			SetParam(strategy, "PriceBreakoutPeriod", priceWindow);
			SetParam(strategy, "VolumeBreakoutPeriod", volumeWindow);
			SetParam(strategy, "TrendlineLength", smaWindow);
			SetParam(strategy, "OrderDirection", direction);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(5).TimeFrame());
			strategy.Volume = 1m;

			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished ||
					candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;
				if (expectedSide is not null)
					errors.Add("The preceding eligible signal was not submitted.");
				expectedSide = null;
				var close = candle.ClosePrice;
				var volume = candle.TotalVolume;
				var hasSma = bars.Count >= smaWindow - 1;
				var sma = hasSma
					? (bars.TakeLast(smaWindow - 1).Sum(x => x.close) + close) / smaWindow
					: 0m;
				var isPending = pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed);
				if (hasSma && !isPending)
				{
					if (actualPosition != 0m)
					{
						var opposite = actualPosition > 0m ? close < sma : close > sma;
						if (opposite)
							oppositeCloses++;
						else
						{
							if (oppositeCloses > 0)
								interruptions++;
							oppositeCloses = 0;
						}
						if (oppositeCloses == 5)
						{
							expectedSide = actualPosition > 0m ? Sides.Sell : Sides.Buy;
							expectedVolume = Math.Abs(actualPosition);
							expectedExits++;
							oppositeCloses = 0;
						}
					}
					else
					{
						oppositeCloses = 0;
						if (bars.Count >= Math.Max(priceWindow, volumeWindow))
						{
							var priorPrice = bars.TakeLast(priceWindow).ToArray();
							var priorVolume = bars.TakeLast(volumeWindow).Max(x => x.volume);
							var longSignal = close > priorPrice.Max(x => x.high) && volume > priorVolume && close > sma;
							var shortSignal = close < priorPrice.Min(x => x.low) && volume > priorVolume && close < sma;
							if (longSignal) qualifiedLongs++;
						if (shortSignal) qualifiedShorts++;
						if (longSignal && direction is "Long" or "Both")
							expectedSide = Sides.Buy;
						else if (shortSignal && direction is "Short" or "Both")
							expectedSide = Sides.Sell;
						if (expectedSide is not null)
						{
							expectedEntries++;
							expectedVolume = strategy.Volume;
						}
						else
							rejects++;
						}
					}
				}
				bars.Add((candle.HighPrice, candle.LowPrice, close, volume));
			};
			strategy.OrderRegistering += order =>
			{
				orders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume ||
					order.Type != OrderTypes.Market || strategy.Position != actualPosition)
					errors.Add($"{strategy.CurrentTime:O}: got {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}, position {actualPosition}.");
				expectedSide = null;
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				fills++;
				actualPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
			};
		}, TimeSpan.FromDays(31), TimeSpan.FromDays(31));

		IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(12)));
		IsTrue(expectedSide is null);
		AreEqual(expectedEntries + expectedExits, orders, "Every order must correspond to exactly one independent signal.");
		IsTrue(fills > 0 && expectedEntries > 0 && rejects > 0,
			$"{direction}/{secondary}: real archive must exercise entries, fills, and rejected signals.");
		TestContext.WriteLine($"1186 {Extension} {direction}, secondary={secondary}: {qualifiedLongs} long / {qualifiedShorts} short qualifying bars, {expectedEntries} entries, {expectedExits} fifth-close exits, {interruptions} interrupted streaks, {orders} orders, {fills} fills.");
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S1243_FullMonthRenkoBuildHasBoundedTickCostAndOrderedBricks()
	{
		async Task<(TimeSpan elapsed, int count, int upward, int downward)> Run(DataType priceSource)
		{
			var errors = new List<string>();
			DateTimeOffset? previousBrickTime = null;
			DateTime? firstReplayTime = null;
			DateTime? lastReplayTime = null;
			var up = 0;
			var down = 0;
			var count = 0;
			var stopwatch = Stopwatch.StartNew();
			await Replay("1243_Renko", (strategy, _) =>
			{
				SetParam(strategy, "BoxSize", 25m);
				SetParam(strategy, "PriceSource", priceSource);
				strategy.Volume = 1m;
				strategy.Connector.CurrentTimeChanged += _ =>
				{
					firstReplayTime ??= strategy.Connector.CurrentTime;
					lastReplayTime = strategy.Connector.CurrentTime;
				};
				strategy.CandleReceived += (_, candle) =>
				{
					if (candle.State != CandleStates.Finished)
						return;
					if (previousBrickTime is { } previous && candle.OpenTime < previous)
						errors.Add("Renko bricks were delivered in noncausal time order.");
					previousBrickTime = candle.OpenTime;
					var movement = candle.ClosePrice - candle.OpenPrice;
					if (Math.Abs(Math.Abs(movement) - 25m) > (strategy.Security.PriceStep ?? 0m))
						errors.Add($"Incomplete or oversized Renko brick: open={candle.OpenPrice}, close={candle.ClosePrice}, box=25.");
					if (movement > 0m) up++;
					else if (movement < 0m) down++;
					else errors.Add("A finished Renko brick has zero direction.");
					count++;
				};
			}, TimeSpan.FromDays(31), TimeSpan.FromDays(31));
			stopwatch.Stop();
			IsTrue(firstReplayTime is not null && lastReplayTime is not null
				&& lastReplayTime.Value - firstReplayTime.Value >= TimeSpan.FromDays(28),
				$"{priceSource}: did not replay the entire packaged month.");
			IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(12)));
			IsTrue(count > 0 && up > 0 && down > 0,
				$"{priceSource}: archive must produce both directions and finished bricks.");
			TestContext.WriteLine($"1243 {Extension}: {priceSource}: {count} finished bricks ({up} up/{down} down), {stopwatch.Elapsed.TotalSeconds:F2}s over the full archived month.");
			return (stopwatch.Elapsed, count, up, down);
		}

		// Different underlying quotes/trades need not produce identical bricks; compare
		// ordering, nonzero brick directions and bounded processing cost instead.
		var level1 = await Run(DataType.Level1);
		var ticks = await Run(DataType.Ticks);
		IsTrue(ticks.elapsed < TimeSpan.FromSeconds(120),
			$"The full-month tick builder took {ticks.elapsed.TotalSeconds:F2}s.");
		IsTrue(ticks.elapsed <= level1.elapsed * 8 + TimeSpan.FromSeconds(30),
			$"Ticks ({ticks.elapsed.TotalSeconds:F2}s) costs disproportionately more than Level1 ({level1.elapsed.TotalSeconds:F2}s) for the same 25-price-unit bricks.");
	}
}
