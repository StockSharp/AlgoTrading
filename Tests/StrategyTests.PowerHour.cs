namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	private const string PowerHour = "1176_Power_Hour_Money";

	[TestMethod]
	[TestCategory("Shard00")]
	public async Task S1176_EntriesWaitForObservedCalendarPeriodOpens()
	{
		var orders = new List<DateTime>();
		await Replay(PowerHour, (strategy, _) => strategy.OrderRegistering += _ => orders.Add(strategy.CurrentTime), TimeSpan.FromDays(31));
		var firstMonth = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Unspecified);
		var firstWeek = firstMonth.AddDays(3);
		var ready = TimeZoneInfo.ConvertTimeToUtc(firstWeek, TimeHelper.Est);
		IsTrue(orders.All(time => time >= ready), "Month, week, day and hour opens need observed boundaries; EMA warmup cannot supply missing historical opens.");
	}

	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(5, 570, 690, false, false, 0.0)]
	[DataRow(5, 480, 960, false, true, 0.0)]
	[DataRow(5, 570, 690, true, false, 0.0)]
	[DataRow(15, 570, 690, true, true, 0.0)]
	[DataRow(5, 570, 690, true, false, 0.01)]
	[DataRow(15, 570, 690, true, false, 0.01)]
	public async Task S1176_CausalFourFrameColorsNySessionsAndActualExits(int frameMinutes, int startMinutes,
		int endMinutes, bool allSessions, bool endOfDay, double trailingPercent)
	{
		var periodOpens = new Dictionary<DateTime, decimal>();
		DateTime? firstObservedTime = null;
		var violations = new List<string>();
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedEntries = 0;
		var entries = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var eodExits = 0;
		var trailingExits = 0;
		var longTrailingExits = 0;
		var shortTrailingExits = 0;
		var intrabarExits = 0;
		var rejectedSession = 0;
		var mixedColors = 0;
		var missingOpens = 0;
		var beforeDst = 0;
		var afterDst = 0;
		var frameCount = 0;
		var entryFrame = 0;
		var extreme = 0m;
		var position = 0m;
		Sides? heldSide = null;
		var trailingTriggered = false;
		DateTime? fillTime = null;
		Order pending = null;
		var start = TimeSpan.FromMinutes(startMinutes);
		var end = TimeSpan.FromMinutes(endMinutes);
		var cutoff = TimeSpan.FromMinutes(1005);
		await Replay(PowerHour, (strategy, _) =>
		{
			foreach (var (name, value) in new (string, object)[]
			{
				("SessionStart", TimeSpan.FromMinutes(570)), ("SessionEnd", TimeSpan.FromMinutes(690)),
				("IsAllSessions", false), ("IsEndOfDayCloseEnabled", false), ("EndOfDayTime", cutoff),
				("IsTrailingEnabled", false), ("LongTrailingPercent", 2m), ("ShortTrailingPercent", 2m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"Power Hour requires {name}, not an EMA crossover proxy.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "CandleType", TimeSpan.FromMinutes(frameMinutes).TimeFrame());
			SetParam(strategy, "SessionStart", start);
			SetParam(strategy, "SessionEnd", end);
			SetParam(strategy, "IsAllSessions", allSessions);
			SetParam(strategy, "IsEndOfDayCloseEnabled", endOfDay);
			SetParam(strategy, "IsTrailingEnabled", trailingPercent > 0.0);
			if (trailingPercent > 0.0)
			{
				SetParam(strategy, "LongTrailingPercent", (decimal)trailingPercent);
				SetParam(strategy, "ShortTrailingPercent", (decimal)trailingPercent);
			}
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add("An eligible calendar consensus was missed.");
				expectedSide = null;
				frameCount++;
				firstObservedTime ??= candle.OpenTime;
				var local = TimeZoneInfo.ConvertTimeFromUtc(candle.OpenTime, TimeHelper.Est);
				var day = local.Date;
				var month = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
				var week = day.AddDays(-(((int)local.DayOfWeek + 6) % 7));
				var keys = new[]
				{
					TimeZoneInfo.ConvertTimeToUtc(month, TimeHelper.Est),
					TimeZoneInfo.ConvertTimeToUtc(week, TimeHelper.Est),
					TimeZoneInfo.ConvertTimeToUtc(day, TimeHelper.Est),
					new DateTime(candle.OpenTime.Year, candle.OpenTime.Month, candle.OpenTime.Day, candle.OpenTime.Hour, 0, 0, DateTimeKind.Utc),
				};
				foreach (var key in keys)
				{
					if (key >= firstObservedTime.Value)
						periodOpens.TryAdd(key, candle.OpenPrice);
				}
				if (keys.Any(key => !periodOpens.ContainsKey(key)))
				{
					missingOpens++;
					return;
				}
				var green = keys.All(key => candle.ClosePrice > periodOpens[key]);
				var red = keys.All(key => candle.ClosePrice < periodOpens[key]);
				if (!green && !red)
					mixedColors++;
				var now = TimeZoneInfo.ConvertTimeFromUtc(strategy.CurrentTime, TimeHelper.Est);
				var sessionAllowed = allSessions || now.TimeOfDay >= start && now.TimeOfDay < end;
				if (!sessionAllowed && (green || red))
					rejectedSession++;
				if (!sessionAllowed || endOfDay && now.TimeOfDay >= cutoff || pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed))
					return;
				if (green && strategy.Position <= 0m)
					expectedSide = Sides.Buy;
				else if (red && strategy.Position >= 0m)
					expectedSide = Sides.Sell;
				if (expectedSide is null)
					return;
				expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
				expectedEntries++;
			};
			strategy.Level1Received += (_, quote) =>
			{
				trailingTriggered = false;
				if (heldSide is not Sides side || trailingPercent <= 0.0)
					return;
				var field = side == Sides.Buy ? Level1Fields.BestBidPrice : Level1Fields.BestAskPrice;
				if (quote.TryGetDecimal(field) is not decimal price || price <= 0m)
					return;
				extreme = side == Sides.Buy ? Math.Max(extreme, price) : Math.Min(extreme, price);
				var stop = extreme * (1m + (side == Sides.Buy ? -1m : 1m) * (decimal)trailingPercent / 100m);
				trailingTriggered = side == Sides.Buy ? price <= stop : price >= stop;
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Type != OrderTypes.Market || position != strategy.Position)
					violations.Add("Orders must use the actual independently accumulated position and market execution.");
				var local = TimeZoneInfo.ConvertTimeFromUtc(strategy.CurrentTime, TimeHelper.Est);
				if (order.Comment == "Power Hour consensus entry")
				{
					entries++;
					if (order.Side != expectedSide || order.Volume != expectedVolume)
						violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume} from four causal candle colors inside NY sessions.");
					if (order.Side == Sides.Buy)
						buys++;
					else
						sells++;
					if (strategy.Position != 0m)
						reversals++;
					expectedSide = null;
				}
				else
				{
					if (strategy.Position == 0m || order.Side != (strategy.Position > 0m ? Sides.Sell : Sides.Buy) || order.Volume != Math.Abs(strategy.Position))
						violations.Add("A protective or EOD order must close the entire held side without reversing.");
					if (order.Comment == "Power Hour end-of-day close")
					{
						eodExits++;
						if (!endOfDay || local.TimeOfDay < cutoff || local.TimeOfDay >= cutoff.Add(TimeSpan.FromMinutes(1)))
							violations.Add("End-of-day closing must occur on the first available market-time event in the NY 16:45 minute.");
						if (TimeHelper.Est.GetUtcOffset(strategy.CurrentTime) == TimeSpan.FromHours(-5))
							beforeDst++;
						else if (TimeHelper.Est.GetUtcOffset(strategy.CurrentTime) == TimeSpan.FromHours(-4))
							afterDst++;
					}
					else if (order.Comment == "Power Hour trailing close")
					{
						trailingExits++;
						if (!trailingTriggered)
							violations.Add("Trailing exits need the current executable bid/ask beyond the independently tracked favorable extreme.");
						if (strategy.Position > 0m)
							longTrailingExits++;
						else
							shortTrailingExits++;
						if (fillTime is DateTime entered && frameCount == entryFrame && strategy.CurrentTime < entered.AddMinutes(4))
							intrabarExits++;
					}
					else
						violations.Add("Every exit must be explicitly explained by optional trailing or EOD closing.");
				}
				pending = order;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				var oldSide = heldSide;
				position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				heldSide = position > 0m ? Sides.Buy : position < 0m ? Sides.Sell : null;
				if (heldSide != oldSide)
					extreme = heldSide is null ? 0m : trade.Trade.Price;
				else if (heldSide is Sides side)
					extreme = side == Sides.Buy ? Math.Max(extreme, trade.Trade.Price) : Math.Min(extreme, trade.Trade.Price);
				fillTime = strategy.CurrentTime;
				entryFrame = frameCount;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null);
		AreEqual(expectedEntries, entries);
		IsTrue(entries > 0 && mixedColors > 0 && missingOpens > 0);
		if (!allSessions)
			IsTrue(rejectedSession > 0);
		if (endOfDay)
			IsTrue(eodExits > 0 && beforeDst > 0 && afterDst > 0, "Real daily exits must exercise both New York UTC offsets in the archive.");
		if (allSessions && trailingPercent == 0.0)
			IsTrue(buys > 0 && sells > 0 && reversals > 0);
		if (trailingPercent > 0.0)
		{
			IsTrue(longTrailingExits > 0 && intrabarExits > 0);
			IsTrue(shortTrailingExits > 0, "The BTC archive must exercise real short-side trailing as well.");
		}
		TestContext.WriteLine($"{PowerHour}, {frameMinutes}m: {entries} entries ({buys}/{sells}), {reversals} reversals, {eodExits} EOD exits ({beforeDst}/{afterDst} before/after DST), {trailingExits} trailing ({longTrailingExits}/{shortTrailingExits}), {intrabarExits} intrabar; {missingOpens} missing-open, {mixedColors} mixed-color, {rejectedSession} outside-session bars.");
	}
}
