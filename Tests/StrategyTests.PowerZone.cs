namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	private const string PowerZone = "1179_PowerZone";

	private sealed class PowerZoneReference(int periods, decimal threshold, bool wicks)
	{
		private readonly List<(decimal open, decimal high, decimal low, decimal close)> _bars = [];
		private readonly HashSet<int> _used = [];
		private (decimal high, decimal low, int stamp)? _bull;
		private (decimal high, decimal low, int stamp)? _bear;
		public int Count => _bars.Count;
		public int BullZones { get; private set; }
		public int BearZones { get; private set; }
		public int ThresholdRejected { get; private set; }
		public int WickDifferences { get; private set; }

		public void Add(ICandleMessage candle)
		{
			_bars.Add((candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice));
			var current = Count - 1;
			var anchor = current - periods - 1;
			if (anchor < 0)
				return;
			var block = _bars[anchor];
			var confirmation = _bars.Skip(anchor + 1).Take(periods).ToArray();
			var bull = block.close < block.open && confirmation.All(bar => bar.close > bar.open);
			var bear = block.close > block.open && confirmation.All(bar => bar.close < bar.open);
			if (!bull && !bear || block.close == 0m)
				return;
			var move = 100m * Math.Abs(confirmation[^1].close - block.close) / Math.Abs(block.close);
			if (move < threshold)
			{
				ThresholdRejected++;
				return;
			}
			if (block.high != Math.Max(block.open, block.close) || block.low != Math.Min(block.open, block.close))
				WickDifferences++;
			var high = wicks ? block.high : Math.Max(block.open, block.close);
			var low = wicks ? block.low : Math.Min(block.open, block.close);
			if (high <= low)
				return;
			if (bull)
			{
				_bull = (high, low, anchor);
				BullZones++;
			}
			else
			{
				_bear = (high, low, anchor);
				BearZones++;
			}
		}

		public Sides? Entry(decimal position)
		{
			if (_bull is { } bull && !_used.Contains(bull.stamp) && position <= 0m && _bars[^1].close > bull.high)
				return Sides.Buy;
			if (_bear is { } bear && !_used.Contains(bear.stamp) && position >= 0m && _bars[^1].close < bear.low)
				return Sides.Sell;
			return null;
		}

		public decimal Consume(Sides side)
		{
			var zone = (side == Sides.Buy ? _bull : _bear).Value;
			_used.Add(zone.stamp);
			return zone.high - zone.low;
		}
	}

	[TestMethod]
	[TestCategory("Shard03")]
	public async Task S1179_DefaultEntriesNeedConfirmedCandleZonesAndFullQuantity()
	{
		var reference = new PowerZoneReference(5, 0m, false);
		var invalid = 0;
		var entries = 0;
		await Replay(PowerZone, (strategy, _) =>
		{
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State == CandleStates.Finished)
					reference.Add(candle);
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Comment is "PowerZone stop loss" or "PowerZone take profit")
					return;
				entries++;
				var side = reference.Entry(strategy.Position);
				if (order.Side != side || order.Volume != strategy.Volume + Math.Abs(strategy.Position))
					invalid++;
				if (side is Sides direction)
					reference.Consume(direction);
			};
		}, TimeSpan.FromDays(31));
		IsTrue(entries > 0);
		AreEqual(0, invalid, "Actual entries need an opposite candle, the completed confirmation sequence, a zone break and a full reversal quantity; EMA orders cannot substitute them.");
	}

	[TestMethod]
	[TestCategory("Shard03")]
	[DataRow(false, 5, 0.0, false, 1.0, 1.5)]
	[DataRow(true, 2, 0.1, true, 0.5, 1.0)]
	[DataRow(false, 3, 0.2, false, 1.0, 2.0)]
	[DataRow(false, 5, 0.0, true, 1.0, 1.5)]
	[DataRow(false, 2, 0.0, false, 0.01, 100.0)]
	[DataRow(false, 2, 0.0, true, 100.0, 0.01)]
	public async Task S1179_ConfirmedBodyOrWickZonesAndFrozenRangeExitsDetermineEveryOrder(bool secondary,
		int periods, double threshold, bool wicks, double stopFactor, double takeFactor)
	{
		var reference = new PowerZoneReference(periods, (decimal)threshold, wicks);
		var violations = new List<string>();
		Sides? expectedSide = null;
		string expectedExit = null;
		var expectedVolume = 0m;
		Order pending = null;
		var expectedEntries = 0;
		var entries = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var stops = 0;
		var takes = 0;
		var intrabar = 0;
		var longExits = 0;
		var shortExits = 0;
		var zonesWhileHeld = 0;
		var position = 0m;
		var basis = 0m;
		var stopDistance = 0m;
		var takeDistance = 0m;
		var entryFrame = 0;
		DateTime? fillTime = null;
		bool isPending() => pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed);
		void expectExit(decimal price)
		{
			if (expectedExit is not null)
				violations.Add("An eligible frozen-zone risk exit was missed.");
			expectedExit = null;
			if (position == 0m || isPending())
				return;
			var profit = (price - basis) * Math.Sign(position);
			if (profit <= -stopDistance) expectedExit = "PowerZone stop loss";
			else if (profit >= takeDistance) expectedExit = "PowerZone take profit";
		}
		await Replay(PowerZone, (strategy, otherSecurity) =>
		{
			if (secondary)
				strategy.Security = otherSecurity;
			foreach (var (name, value) in new (string, object)[]
			{
				("Periods", 5), ("Threshold", 0m), ("UseWicks", false), ("TakeProfitFactor", 1.5m), ("StopLossFactor", 1m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"PowerZone requires the published {name}, not an EMA proxy.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var (name, value) in new (string, object)[]
			{
				("Periods", periods), ("Threshold", (decimal)threshold), ("UseWicks", wicks),
				("StopLossFactor", (decimal)stopFactor), ("TakeProfitFactor", (decimal)takeFactor),
			})
				SetParam(strategy, name, value);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add("An eligible confirmed-zone entry was missed.");
				expectedSide = null;
				var zoneCount = reference.BullZones + reference.BearZones;
				reference.Add(candle);
				if (position != 0m)
					zonesWhileHeld += reference.BullZones + reference.BearZones - zoneCount;
				expectExit(candle.ClosePrice);
				if (expectedExit is not null || isPending())
					return;
				expectedSide = reference.Entry(position);
				if (expectedSide is null)
					return;
				expectedVolume = strategy.Volume + Math.Abs(position);
				expectedEntries++;
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
				if (order.Type != OrderTypes.Market || strategy.Position != position)
					violations.Add("Orders require market execution and the actual independently filled position.");
				if (order.Comment == "PowerZone breakout entry")
				{
					if (order.Side != expectedSide || order.Volume != expectedVolume || expectedExit is not null)
						violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume} from confirmed, unconsumed candle zones.");
					var width = reference.Consume(order.Side);
					stopDistance = width * (decimal)stopFactor;
					takeDistance = width * (decimal)takeFactor;
					entries++;
					if (order.Side == Sides.Buy) buys++; else sells++;
					if (position != 0m) reversals++;
					expectedSide = null;
				}
				else
				{
					if (order.Comment != expectedExit || position == 0m || order.Side != (position > 0m ? Sides.Sell : Sides.Buy) || order.Volume != Math.Abs(position))
						violations.Add($"{order.Comment} must equal {expectedExit} from the frozen entry-zone width and actual fill, and close the whole position.");
					if (order.Comment == "PowerZone stop loss") stops++; else if (order.Comment == "PowerZone take profit") takes++;
					if (position > 0m) longExits++; else shortExits++;
					if (fillTime is DateTime entered && reference.Count == entryFrame && strategy.CurrentTime < entered.AddMinutes(4)) intrabar++;
					expectedExit = null;
				}
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				var previous = position;
				var signed = trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				position += signed;
				if (position == 0m) basis = 0m;
				else if (previous == 0m || Math.Sign(position) != Math.Sign(previous)) basis = trade.Trade.Price;
				else if (Math.Sign(signed) == Math.Sign(previous)) basis = (basis * Math.Abs(previous) + trade.Trade.Price * Math.Abs(signed)) / Math.Abs(position);
				if (position != 0m) { fillTime = strategy.CurrentTime; entryFrame = reference.Count; }
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null && expectedExit is null);
		AreEqual(expectedEntries, entries);
		IsTrue(buys > 0 && sells > 0 && reference.BullZones > 0 && reference.BearZones > 0 && reference.WickDifferences > 0);
		if (threshold > 0.0) IsTrue(reference.ThresholdRejected > 0);
		IsTrue(stops + takes > 0 && longExits > 0 && shortExits > 0);
		if (stopFactor == 0.01 || takeFactor == 0.01)
		{
			IsTrue(intrabar > 0);
			if (stopFactor == 0.01) IsTrue(stops > 0); else IsTrue(takes > 0);
		}
		if (takeFactor == 0.01)
			IsTrue(zonesWhileHeld > 0, "Real new zones must appear while a position is held; its risk distances must still use the frozen entry-zone range.");
		TestContext.WriteLine($"{PowerZone}: {entries} entries ({buys}/{sells}), {reversals} reversals, {stops} SL/{takes} TP, {longExits}/{shortExits} long/short exits, {intrabar} intrabar, {reference.BullZones}/{reference.BearZones} bull/bear zones, {reference.ThresholdRejected} threshold rejections, {reference.WickDifferences} body/wick differences, {zonesWhileHeld} new zones while holding.");
	}
}
