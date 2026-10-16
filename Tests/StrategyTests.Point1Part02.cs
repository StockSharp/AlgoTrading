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
						if (longSignal && (direction is "Long" or "Both"))
							expectedSide = Sides.Buy;
						else if (shortSignal && (direction is "Short" or "Both"))
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
		async Task<(TimeSpan elapsed, int count, int upward, int downward)> Point1Part02Run(DataType priceSource)
		{
			var errors = new List<string>();
			DateTimeOffset? previousBrickTime = null;
			DateTime? firstReplayTime = null;
			DateTime? lastReplayTime = null;
			var up = 0;
			var down = 0;
			var count = 0;
			var previousDirection = 0;
			var netPosition = 0m;
			Sides? expectedSide = null;
			var expectedVolume = 0m;
			var orders = 0;
			var fills = 0;
			var expectedReversals = 0;
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
					if (expectedSide is not null)
						errors.Add("A previous Renko direction reversal did not register its order.");
					expectedSide = null;
					var direction = Math.Sign(movement);
					if (previousDirection != 0 && direction != previousDirection
						&& (direction > 0 ? netPosition <= 0m : netPosition >= 0m))
					{
						expectedSide = direction > 0 ? Sides.Buy : Sides.Sell;
						expectedVolume = strategy.Volume + Math.Abs(netPosition);
						expectedReversals++;
					}
					previousDirection = direction;
				};
				strategy.OrderRegistering += order =>
				{
					orders++;
					if (order.Side != expectedSide || order.Volume != expectedVolume
						|| order.Type != OrderTypes.Market || strategy.Position != netPosition)
						errors.Add($"{strategy.CurrentTime:O}: Renko order {order.Side}/{order.Volume}; expected {expectedSide}/{expectedVolume} at position {netPosition}.");
					expectedSide = null;
				};
				strategy.OwnTradeReceived += (_, trade) =>
				{
					fills++;
					netPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				};
			}, TimeSpan.FromDays(31), TimeSpan.FromDays(31));
			stopwatch.Stop();
			IsTrue(firstReplayTime is not null && lastReplayTime is not null
				&& lastReplayTime.Value - firstReplayTime.Value >= TimeSpan.FromDays(28),
				$"{priceSource}: did not replay the entire packaged month.");
			IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(12)));
			AreEqual(expectedReversals, orders, "All and only completed Renko direction reversals may issue market orders.");
			IsTrue(expectedSide is null && orders > 0 && fills > 0);
			IsTrue(count > 0 && up > 0 && down > 0,
				$"{priceSource}: archive must produce both directions and finished bricks.");
			TestContext.WriteLine($"1243 {Extension}: {priceSource}: {count} finished bricks ({up} up/{down} down), {orders} reversal orders/{fills} fills, {stopwatch.Elapsed.TotalSeconds:F2}s over the full archived month.");
			return (stopwatch.Elapsed, count, up, down);
		}

		// Different underlying quotes/trades need not produce identical bricks; compare
		// ordering, nonzero brick directions and bounded processing cost instead.
		var level1 = await Point1Part02Run(DataType.Level1);
		var ticks = await Point1Part02Run(DataType.Ticks);
		IsTrue(ticks.elapsed < TimeSpan.FromSeconds(120),
			$"The full-month tick builder took {ticks.elapsed.TotalSeconds:F2}s.");
		IsTrue(ticks.elapsed <= level1.elapsed * 8 + TimeSpan.FromSeconds(30),
			$"Ticks ({ticks.elapsed.TotalSeconds:F2}s) costs disproportionately more than Level1 ({level1.elapsed.TotalSeconds:F2}s) for the same 25-price-unit bricks.");
	}

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0779_BollingerBreakoutsAndBandReentryExitsMatchIndependentBands(bool secondary)
	{
		const int length = 10;
		const decimal deviation = 1.5m;
		var window = new Queue<decimal>();
		decimal? previousClose = null;
		decimal? previousUpper = null;
		decimal? previousLower = null;
		var position = 0m;
		Order pending = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var actualOrders = 0;
		var fills = 0;
		var entriesLong = 0;
		var entriesShort = 0;
		var exits = 0;
		var ignored = 0;
		var failures = new List<string>();

		await Replay("0779_Fancy_Bollinger_Bands", (strategy, other) =>
		{
			AreEqual(20, strategy.Parameters["BollingerPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["DeviationMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			if (secondary) strategy.Security = other;
			SetParam(strategy, "BollingerPeriod", length);
			SetParam(strategy, "DeviationMultiplier", deviation);
			strategy.Volume = 1m;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;
				if (expectedSide is not null)
					failures.Add("Eligible Bollinger entry or band reentry exit not submitted.");
				expectedSide = null;
				window.Enqueue(candle.ClosePrice);
				if (window.Count > length)
					window.Dequeue();
				if (window.Count < length)
					return;
				var mean = window.Average();
				var sigma = (decimal)Math.Sqrt((double)(window.Sum(price => (price - mean) * (price - mean)) / length));
				var upper = mean + deviation * sigma;
				var lower = mean - deviation * sigma;
				var hasPreviousBands = previousUpper is not null && previousLower is not null && previousClose is not null;
				if (hasPreviousBands && (pending is null || pending.State is OrderStates.Done or OrderStates.Failed))
				{
					if (position > 0m && previousClose >= previousUpper && candle.ClosePrice <= upper)
					{
						expectedSide = Sides.Sell;
						expectedVolume = position;
						exits++;
					}
					else if (position < 0m && previousClose <= previousLower && candle.ClosePrice >= lower)
					{
						expectedSide = Sides.Buy;
						expectedVolume = -position;
						exits++;
					}
					else if (position == 0m && previousClose <= previousUpper && candle.ClosePrice > upper)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume;
						entriesLong++;
					}
					else if (position == 0m && previousClose >= previousLower && candle.ClosePrice < lower)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Volume;
						entriesShort++;
					}
					else ignored++;
				}
				previousUpper = upper;
				previousLower = lower;
				previousClose = candle.ClosePrice;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume
					|| order.Type != OrderTypes.Market || position != strategy.Position)
					failures.Add($"{strategy.CurrentTime:O}: unexpected {order.Side}/{order.Volume}; expected {expectedSide}/{expectedVolume}.");
				expectedSide = null;
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				fills++;
				position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
			};
		}, TimeSpan.FromDays(31), TimeSpan.FromDays(1));

		IsTrue(failures.Count == 0, string.Join(Environment.NewLine, failures.Take(12)));
		AreEqual(entriesLong + entriesShort + exits, actualOrders, "Orders must exactly match independent bands and crossover edges.");
		IsTrue(fills > 0 && entriesLong > 0 && entriesShort > 0 && exits > 0 && ignored > 0,
			"Packaged month must exercise both breakout directions, re-entry exits and quiet bars.");
		TestContext.WriteLine($"0779 {Extension} TON={secondary}: {entriesLong} longs, {entriesShort} shorts, {exits} exits, {fills} fills, {ignored} no-signal candles.");
	}

	[TestMethod]
	[TestCategory("Shard02")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0778_FalconUsesPriorSwingWicksSmaAndNativeTickProtection(bool secondary)
	{
		const int swing = 3;
		const int trend = 5;
		var highs = new Queue<decimal>();
		var lows = new Queue<decimal>();
		var closes = new Queue<decimal>();
		var netPosition = 0m;
		var entryOrders = 0;
		var exitOrders = 0;
		var fills = 0;
		var allowedEntries = 0;
		var disallowedBars = 0;
		Sides? expectedSide = null;
		Order pending = null;
		var errors = new List<string>();
		await Replay("0778_Falcon_Liquidity_Grab", (strategy, other) =>
		{
			AreEqual(20, strategy.Parameters["SwingPeriod"].Value);
			AreEqual(50, strategy.Parameters["TrendLength"].Value);
			AreEqual(50, strategy.Parameters["StopLossPoints"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["TakeProfitMultiplier"].Value));
			AreEqual(true, strategy.Parameters["UseSession"].Value);
			AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
			if (secondary) strategy.Security = other;
			SetParam(strategy, "SwingPeriod", swing);
			SetParam(strategy, "TrendLength", trend);
			SetParam(strategy, "UseSession", false);
			SetParam(strategy, "StopLossPoints", 10);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;
				if (expectedSide is not null)
					errors.Add("Falcon missed its price-wick/SMA entry.");
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				while (closes.Count > trend) closes.Dequeue();
				var formed = closes.Count == trend && highs.Count == swing && lows.Count == swing;
				var pendingActive = pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed);
				if (formed && netPosition == 0m && !pendingActive)
				{
					var sma = closes.Average();
					if (candle.LowPrice < lows.Min() && candle.ClosePrice > sma)
						expectedSide = Sides.Buy;
					else if (candle.HighPrice > highs.Max() && candle.ClosePrice < sma)
						expectedSide = Sides.Sell;
					if (expectedSide is null) disallowedBars++;
					else allowedEntries++;
				}
				highs.Enqueue(candle.HighPrice);
				lows.Enqueue(candle.LowPrice);
				while (highs.Count > swing) highs.Dequeue();
				while (lows.Count > swing) lows.Dequeue();
			};
			strategy.OrderRegistering += order =>
			{
				if (netPosition == 0m)
				{
					entryOrders++;
					if (order.Side != expectedSide || order.Type != OrderTypes.Market || order.Volume != strategy.Volume)
						errors.Add($"Falcon registered unsignaled entry {order.Side}/{order.Volume}; expected {expectedSide}/{strategy.Volume}.");
				}
				else
				{
					exitOrders++;
					if (order.Side != (netPosition > 0m ? Sides.Sell : Sides.Buy)
						|| order.Volume != Math.Abs(netPosition))
						errors.Add("Falcon protection must close the entire current position.");
				}
				expectedSide = null;
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				fills++;
				netPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
			};
		}, TimeSpan.FromDays(31), TimeSpan.FromHours(4));
		IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(12)));
		IsTrue(expectedSide is null);
		AreEqual(allowedEntries, entryOrders, "There must be one entry for every eligible wick and none for ineligible bars.");
		IsTrue(entryOrders > 0 && fills > 0 && disallowedBars > 0);
		TestContext.WriteLine($"0778 {Extension}: other={secondary}: {entryOrders} entries, {exitOrders} native exits, {fills} fills and {disallowedBars} rejected bars.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(false)]
	[DataRow(true)]
	public async Task S0781_IndependentFibBandEntryAndPriorSwingHighExit(bool secondary)
	{
		const int bandLength = 8;
		const int swingWindow = 12;
		const decimal bandFactor = 1.0m;
		const decimal fibFactor = 0.382m;
		var closes = new Queue<decimal>();
		var highs = new Queue<decimal>();
		var lows = new Queue<decimal>();
		var position = 0m;
		Order pending = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedEntries = 0;
		var expectedExits = 0;
		var nonSignals = 0;
		var orders = 0;
		var fills = 0;
		var errors = new List<string>();
		await Replay("0781_Fibonacci_Bollinger_Bands", (strategy, other) =>
		{
			AreEqual(20, strategy.Parameters["BollingerPeriod"].Value);
			AreEqual(2m, Convert.ToDecimal(strategy.Parameters["BollingerDeviation"].Value));
			AreEqual(50, strategy.Parameters["FibLookback"].Value);
			AreEqual(0.618m, Convert.ToDecimal(strategy.Parameters["FibonacciLevel"].Value));
			AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
			if (secondary) strategy.Security = other;
			SetParam(strategy, "BollingerPeriod", bandLength);
			SetParam(strategy, "BollingerDeviation", bandFactor);
			SetParam(strategy, "FibLookback", swingWindow);
			SetParam(strategy, "FibonacciLevel", fibFactor);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished || candle.SecurityId != strategy.Security.Id.ToSecurityId())
					return;
				if (expectedSide is not null)
					errors.Add("A previous Fibonacci/Bollinger signal was not submitted.");
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				while (closes.Count > bandLength) closes.Dequeue();
				var complete = closes.Count == bandLength && highs.Count == swingWindow && lows.Count == swingWindow;
				var pendingActive = pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed);
				if (complete && !pendingActive)
				{
					var mean = closes.Average();
					var variance = closes.Sum(price => (price - mean) * (price - mean)) / bandLength;
					var sd = (decimal)Math.Sqrt((double)variance);
					var upper = mean + sd * bandFactor;
					var lower = mean - sd * bandFactor;
					var swingHigh = highs.Max();
					var swingLow = lows.Min();
					var fib = swingLow + fibFactor * (swingHigh - swingLow);
					if (position > 0m && (candle.ClosePrice >= upper || candle.ClosePrice >= swingHigh))
					{
						expectedSide = Sides.Sell;
						expectedVolume = position;
						expectedExits++;
					}
					else if (position == 0m && candle.ClosePrice < lower && candle.ClosePrice > fib)
					{
						expectedSide = Sides.Buy;
						expectedVolume = strategy.Volume;
						expectedEntries++;
					}
					else nonSignals++;
				}
				highs.Enqueue(candle.HighPrice);
				lows.Enqueue(candle.LowPrice);
				while (highs.Count > swingWindow) highs.Dequeue();
				while (lows.Count > swingWindow) lows.Dequeue();
			};
			strategy.OrderRegistering += order =>
			{
				orders++;
				if (expectedSide != order.Side || expectedVolume != order.Volume ||
					order.Type != OrderTypes.Market || position != strategy.Position)
					errors.Add($"Unexpected Fib/Bollinger order {order.Side}/{order.Volume}; expected {expectedSide}/{expectedVolume}.");
				expectedSide = null;
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				fills++;
				position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				if (position < 0m)
					errors.Add("Fib/Bollinger is long-only and may not open a short position.");
			};
		}, TimeSpan.FromDays(31), TimeSpan.FromDays(1));
		IsTrue(errors.Count == 0, string.Join(Environment.NewLine, errors.Take(12)));
		AreEqual(expectedEntries + expectedExits, orders);
		IsTrue(expectedEntries > 0 && expectedExits > 0 && fills > 0 && nonSignals > 0,
			"Existing history must exercise long entries, exits, filled orders and no-signal bars.");
		TestContext.WriteLine($"0781 {Extension} secondary={secondary}: {expectedEntries} entries, {expectedExits} exits, {fills} fills, {nonSignals} ignored bars.");
	}
}
