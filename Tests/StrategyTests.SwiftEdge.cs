namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

public abstract partial class StrategyTests
{
	private const string SwiftEdge = "1177_PowerHouse_SwiftEdge_AI_v2_10";

	[TestMethod]
	[TestCategory("Shard01")]
	public async Task S1177_DefaultOrdersRequireConfirmedPivotBreakout()
	{
		var frames = new List<(decimal high, decimal low, decimal close)>();
		decimal? pivotHigh = null;
		decimal? pivotLow = null;
		var up = false;
		var down = false;
		var entries = 0;
		var invalidEntries = 0;
		int? lastSignal = null;
		await Replay(SwiftEdge, (strategy, _) =>
		{
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				frames.Add((candle.HighPrice, candle.LowPrice, candle.ClosePrice));
				var current = frames.Count - 1;
				if (current >= 4)
				{
					var neighbors = frames.Skip(current - 4).Take(5).Where((_, offset) => offset != 2).ToArray();
					if (frames[current - 2].high > neighbors.Max(frame => frame.high))
						pivotHigh = frames[current - 2].high;
					if (frames[current - 2].low < neighbors.Min(frame => frame.low))
						pivotLow = frames[current - 2].low;
				}
				up = down = false;
				if (current < 10 || lastSignal is int signal && current - signal < 5)
					return;
				up = pivotHigh is decimal high && frames[current - 1].close <= high && candle.ClosePrice > high && candle.ClosePrice > frames[current - 10].close;
				down = pivotLow is decimal low && frames[current - 1].close >= low && candle.ClosePrice < low && candle.ClosePrice < frames[current - 10].close;
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Comment is "SwiftEdge stop loss" or "SwiftEdge take profit")
					return;
				entries++;
				if (order.Side == Sides.Buy ? !up : !down)
					invalidEntries++;
				lastSignal = frames.Count - 1;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(entries > 0);
		AreEqual(0, invalidEntries, "Every actual entry must cross a confirmed pivot with signed momentum and sufficient finished-bar spacing; EMA crosses are insufficient.");
	}

	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(false, 2, 10, 5, 0.0, 50.0, 100.0)]
	[DataRow(true, 3, 6, 12, 0.01, 0.15, 0.3)]
	[DataRow(false, 4, 20, 0, 5.0, 100.0, 200.0)]
	[DataRow(false, 2, 10, 50, 0.0, 50.0, 100.0)]
	[DataRow(false, 2, 10, 5, 0.0, 0.01, 10000.0)]
	[DataRow(false, 2, 10, 5, 0.0, 10000.0, 0.01)]
	public async Task S1177_ConfirmedPivotBreakoutsMomentumSpacingAndPointExitsDetermineEveryOrder(bool secondary,
		int pivotPeriod, int momentumPeriod, int minSignalBars, double threshold, double stopPoints, double takePoints)
	{
		var frames = new List<(decimal high, decimal low, decimal close)>();
		var violations = new List<string>();
		decimal? pivotHigh = null;
		decimal? pivotLow = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedEntries = 0;
		string expectedExit = null;
		var entries = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var stops = 0;
		var takes = 0;
		var longExits = 0;
		var shortExits = 0;
		var intrabarExits = 0;
		var spacingRejected = 0;
		var momentumRejected = 0;
		var confirmedHighs = 0;
		var confirmedLows = 0;
		int? lastSignal = null;
		Order pending = null;
		var position = 0m;
		var basis = 0m;
		var entryFrame = 0;
		var stop = (decimal)stopPoints;
		var take = (decimal)takePoints;
		var momentumThreshold = (decimal)threshold;
		DateTime? fillTime = null;
		bool isPending() => pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed);
		void expectExit(decimal price)
		{
			if (expectedExit is not null)
				violations.Add("An eligible point stop/take was missed on the previous price event.");
			expectedExit = null;
			if (position == 0m || isPending())
				return;
			var profit = (price - basis) * Math.Sign(position);
			if (profit <= -stop)
				expectedExit = "SwiftEdge stop loss";
			else if (profit >= take)
				expectedExit = "SwiftEdge take profit";
		}
		await Replay(SwiftEdge, (strategy, otherSecurity) =>
		{
			if (secondary)
				strategy.Security = otherSecurity;
			foreach (var (name, value) in new (string, object)[]
			{
				("PivotPeriod", 2), ("MomentumPeriod", 10), ("MomentumThreshold", 0m),
				("MinSignalBars", 5), ("StopLossPoints", 50m), ("TakeProfitPoints", 100m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"SwiftEdge requires {name}; EMA crossover is not pivot breakout with point protection.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var (name, value) in new (string, object)[]
			{
				("PivotPeriod", pivotPeriod), ("MomentumPeriod", momentumPeriod), ("MomentumThreshold", momentumThreshold),
				("MinSignalBars", minSignalBars), ("StopLossPoints", stop), ("TakeProfitPoints", take),
			})
				SetParam(strategy, name, value);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add("An eligible confirmed-pivot breakout was missed.");
				expectedSide = null;
				frames.Add((candle.HighPrice, candle.LowPrice, candle.ClosePrice));
				var current = frames.Count - 1;
				if (current >= 2 * pivotPeriod)
				{
					var candidate = current - pivotPeriod;
					var neighbors = frames.Skip(candidate - pivotPeriod).Take(2 * pivotPeriod + 1)
						.Where((_, offset) => offset != pivotPeriod).ToArray();
					if (frames[candidate].high > neighbors.Max(frame => frame.high))
					{
						pivotHigh = frames[candidate].high;
						confirmedHighs++;
					}
					if (frames[candidate].low < neighbors.Min(frame => frame.low))
					{
						pivotLow = frames[candidate].low;
						confirmedLows++;
					}
				}
				expectExit(candle.ClosePrice);
				if (current < momentumPeriod || current < 1 || expectedExit is not null || isPending())
					return;
				var previous = frames[current - 1].close;
				var up = pivotHigh is decimal high && previous <= high && candle.ClosePrice > high;
				var down = pivotLow is decimal low && previous >= low && candle.ClosePrice < low;
				var momentum = candle.ClosePrice - frames[current - momentumPeriod].close;
				if (up && momentum <= momentumThreshold || down && momentum >= -momentumThreshold)
					momentumRejected++;
				if (!up && !down)
					return;
				if (lastSignal is int signal && current - signal < minSignalBars)
				{
					spacingRejected++;
					return;
				}
				if (up && momentum > momentumThreshold && strategy.Position <= 0m)
					expectedSide = Sides.Buy;
				else if (down && momentum < -momentumThreshold && strategy.Position >= 0m)
					expectedSide = Sides.Sell;
				if (expectedSide is not null)
				{
					expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
					expectedEntries++;
				}
			};
			strategy.Level1Received += (_, quote) =>
			{
				if (position == 0m)
					return;
				var field = position > 0m ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
				if (quote.TryGetDecimal(field) is decimal price && price > 0m)
					expectExit(price);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Type != OrderTypes.Market || position != strategy.Position)
					violations.Add("Every order requires market execution and actual independently accumulated position.");
				if (order.Comment == "SwiftEdge pivot breakout")
				{
					if (order.Side != expectedSide || order.Volume != expectedVolume || expectedExit is not null)
						violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume} from confirmed pivots, causal momentum and bar spacing.");
					entries++;
					if (order.Side == Sides.Buy) buys++; else sells++;
					if (position != 0m) reversals++;
					lastSignal = frames.Count - 1;
					expectedSide = null;
				}
				else
				{
					if (order.Comment != expectedExit || position == 0m || order.Side != (position > 0m ? Sides.Sell : Sides.Buy) || order.Volume != Math.Abs(position))
						violations.Add($"Point exit {order.Comment} must equal {expectedExit} at the current price and close the exact held quantity.");
					if (order.Comment == "SwiftEdge stop loss") stops++;
					else if (order.Comment == "SwiftEdge take profit") takes++;
					if (position > 0m) longExits++; else shortExits++;
					if (fillTime is DateTime entered && frames.Count == entryFrame && strategy.CurrentTime < entered.AddMinutes(4))
						intrabarExits++;
					expectedExit = null;
				}
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				var oldPosition = position;
				var signed = trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				position += signed;
				if (position == 0m)
					basis = 0m;
				else if (oldPosition == 0m || Math.Sign(position) != Math.Sign(oldPosition))
					basis = trade.Trade.Price;
				else if (Math.Sign(signed) == Math.Sign(oldPosition))
					basis = (basis * Math.Abs(oldPosition) + trade.Trade.Price * Math.Abs(signed)) / Math.Abs(position);
				if (position != 0m)
				{
					fillTime = strategy.CurrentTime;
					entryFrame = frames.Count;
				}
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null && expectedExit is null);
		AreEqual(expectedEntries, entries);
		IsTrue(buys > 0 && sells > 0 && confirmedHighs > 0 && confirmedLows > 0 && momentumRejected > 0);
		if (minSignalBars >= 50)
			IsTrue(spacingRejected > 0, "The long minimum interval must suppress otherwise eligible breakouts.");
		if (minSignalBars == 0)
			AreEqual(0, spacingRejected, "Zero spacing must not suppress a confirmed breakout.");
		if (secondary)
			IsTrue(reversals > 0, "TON must exercise real full opposite-position reversals, not only flat entries.");
		IsTrue(stops + takes > 0 && longExits > 0 && shortExits > 0);
		if (stopPoints == 0.01 || takePoints == 0.01)
		{
			IsTrue(intrabarExits > 0, "Tight point protection must execute between finished candles on real quotes.");
			if (stopPoints == 0.01) IsTrue(stops > 0); else IsTrue(takes > 0);
		}
		TestContext.WriteLine($"{SwiftEdge}: {entries} entries ({buys}/{sells}), {reversals} reversals, {stops} SL / {takes} TP, {longExits}/{shortExits} long/short exits, {intrabarExits} intrabar, {confirmedHighs}/{confirmedLows} confirmed pivots, {momentumRejected} momentum and {spacingRejected} spacing rejections.");
	}
}
