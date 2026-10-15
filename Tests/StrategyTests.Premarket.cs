namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	private const string Premarket = "1181_Premarket_Gap_MomoTrader";

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(false, 0.05, 0.0)]
	[DataRow(true, 5.0, 15000.0)]
	public async Task S1181_ArchiveOrdersMustFollowLongOnlyPremarketMomentum(bool secondary, double minGain, double minVolume)
	{
		ICandleMessage previous = null;
		ICandleMessage current = null;
		var candidates = 0;
		var invalidOrders = 0;
		var maximumGain = 0m;
		var tradedDates = new HashSet<DateTime>();
		await Replay(Premarket, (strategy, other) =>
		{
			if (secondary)
				strategy.Security = other;
			if (strategy.Parameters.ContainsKey("MinGainPct"))
				SetParam(strategy, "MinGainPct", (decimal)minGain);
			if (strategy.Parameters.ContainsKey("MinVolume"))
				SetParam(strategy, "MinVolume", (decimal)minVolume);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				previous = current;
				current = (ICandleMessage)((Message)candle).Clone();
				if (previous is null || previous.ClosePrice <= 0m)
					return;
				var gain = 100m * (current.ClosePrice - previous.ClosePrice) / previous.ClosePrice;
				maximumGain = Math.Max(maximumGain, gain);
				var time = TimeZoneInfo.ConvertTimeFromUtc(strategy.CurrentTime, TimeHelper.Est).TimeOfDay;
				if (time >= TimeSpan.FromHours(4) && time < TimeSpan.FromMinutes(570)
					&& gain >= (decimal)minGain && current.TotalVolume > (decimal)minVolume && current.ClosePrice > current.OpenPrice
					&& current.HighPrice > current.LowPrice && (current.ClosePrice - current.OpenPrice) * 100m >= (current.HighPrice - current.LowPrice) * 75m)
					candidates++;
			};
			strategy.OrderRegistering += order =>
			{
				var local = TimeZoneInfo.ConvertTimeFromUtc(strategy.CurrentTime, TimeHelper.Est);
				if (order.Side == Sides.Sell)
				{
					if (strategy.Position <= 0m || order.Volume != strategy.Position || current is null || previous is null
						|| current.ClosePrice >= current.OpenPrice && current.TotalVolume > previous.TotalVolume)
						invalidOrders++;
				}
				else if (current is null || previous is null || previous.ClosePrice <= 0m || strategy.Position != 0m
					|| local.TimeOfDay < TimeSpan.FromHours(4) || local.TimeOfDay >= TimeSpan.FromMinutes(570)
					|| 100m * (current.ClosePrice - previous.ClosePrice) < (decimal)minGain * previous.ClosePrice
					|| current.TotalVolume <= (decimal)minVolume || current.ClosePrice <= current.OpenPrice
					|| (current.ClosePrice - current.OpenPrice) * 100m < (current.HighPrice - current.LowPrice) * 75m
					|| !tradedDates.Add(local.Date))
					invalidOrders++;
			};
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"{Premarket}: secondary={secondary}, thresholds={minGain}%/{minVolume}, {candidates} qualified candles, maximum five-minute close gain {maximumGain}%, {invalidOrders} invalid actual orders.");
		IsTrue(candidates > 0, "The archive fixture must contain qualifying momentum candles.");
		AreEqual(0, invalidOrders, "EMA orders do not implement long-only momentum, volume/body filters, premarket hours or one entry per New York day.");
	}

	[TestMethod]
	[TestCategory("Shard05")]
	public Task S1181_PremarketGapMomoTrader()
		=> Replay(Premarket, (strategy, secondary) => strategy.Security = secondary, TimeSpan.FromDays(31));

	[TestMethod]
	[TestCategory("Shard05")]
	[DataRow(false, false, 0.05, 0.0)]
	[DataRow(false, true, 0.05, 10.0)]
	[DataRow(true, false, 0.2, 0.0)]
	[DataRow(true, true, 0.2, 1000.0)]
	public async Task S1181_BodyTiersDailyLimitAndEveryMomentumExit(bool secondary, bool useSession, double minGain, double minVolume)
	{
		ICandleMessage previous = null;
		ICandleMessage current = null;
		var violations = new List<string>();
		var enteredDates = new HashSet<DateTime>();
		var tierCounts = new int[3];
		var entries = 0;
		var exits = 0;
		var redExits = 0;
		var volumeExits = 0;
		var increasingBullishHolds = 0;
		var dailyRejected = 0;
		var sessionRejected = 0;
		var preDstEntries = 0;
		var postDstEntries = 0;
		var position = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var entryTier = 0;
		Order pending = null;
		await Replay(Premarket, (strategy, other) =>
		{
			if (secondary)
				strategy.Security = other;
			foreach (var (name, value) in new (string, object)[]
			{
				("MinGainPct", 5m), ("MinVolume", 15000m), ("UseSession", true),
				("SessionStart", TimeSpan.FromHours(4)), ("SessionEnd", TimeSpan.FromMinutes(570)),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"The published momentum strategy needs {name}.");
				AreEqual(value, strategy.Parameters[name].Value);
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "MinGainPct", (decimal)minGain);
			SetParam(strategy, "MinVolume", (decimal)minVolume);
			SetParam(strategy, "UseSession", useSession);
			strategy.Volume = 1.3m;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add("An eligible momentum entry or required exit was missed.");
				expectedSide = null;
				previous = current;
				current = (ICandleMessage)((Message)candle).Clone();
				if (previous is null || pending is not null && pending.State is not (OrderStates.Done or OrderStates.Failed))
					return;
				if (position > 0m)
				{
					if (current.ClosePrice < current.OpenPrice || current.TotalVolume <= previous.TotalVolume)
					{
						expectedSide = Sides.Sell;
						expectedVolume = position;
						if (current.ClosePrice < current.OpenPrice)
							redExits++;
						else
							volumeExits++;
					}
					else if (current.ClosePrice > current.OpenPrice)
						increasingBullishHolds++;
					return;
				}
				if (previous.ClosePrice <= 0m || current.ClosePrice <= current.OpenPrice || current.HighPrice <= current.LowPrice
					|| current.TotalVolume <= (decimal)minVolume || 100m * (current.ClosePrice - previous.ClosePrice) < (decimal)minGain * previous.ClosePrice)
					return;
				var body = 100m * (current.ClosePrice - current.OpenPrice) / (current.HighPrice - current.LowPrice);
				entryTier = body >= 90m ? 0 : body >= 85m ? 1 : body >= 75m ? 2 : -1;
				if (entryTier < 0)
					return;
				var local = TimeZoneInfo.ConvertTimeFromUtc(strategy.CurrentTime, TimeHelper.Est);
				if (useSession && (local.TimeOfDay < TimeSpan.FromHours(4) || local.TimeOfDay >= TimeSpan.FromMinutes(570)))
				{
					sessionRejected++;
					return;
				}
				if (enteredDates.Contains(local.Date))
				{
					dailyRejected++;
					return;
				}
				var desired = strategy.Volume / (entryTier == 0 ? 1m : entryTier == 1 ? 2m : 4m);
				var step = strategy.Security.VolumeStep ?? 1m;
				expectedVolume = decimal.Truncate(desired / step) * step;
				if (expectedVolume < (strategy.Security.MinVolume ?? step))
					return;
				expectedSide = Sides.Buy;
			};
			strategy.OrderRegistering += order =>
			{
				if (order.Side != expectedSide || order.Type != OrderTypes.Market || order.Volume != expectedVolume || strategy.Position != position)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}, expected {expectedSide} {expectedVolume}.");
				if (order.Side == Sides.Buy)
				{
					entries++;
					var local = TimeZoneInfo.ConvertTimeFromUtc(strategy.CurrentTime, TimeHelper.Est);
					if (!enteredDates.Add(local.Date))
						violations.Add("More than one entry per New York date.");
					if (entryTier >= 0)
						tierCounts[entryTier]++;
					if (local.Date < new DateTime(2024, 3, 10))
						preDstEntries++;
					else
						postDstEntries++;
				}
				else
					exits++;
				pending = order;
				expectedSide = null;
			};
			strategy.OwnTradeReceived += (_, trade) => position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
		}, TimeSpan.FromDays(31));
		TestContext.WriteLine($"{Premarket}: {entries} entries/{exits} exits, tiers {string.Join("/", tierCounts)}, red/volume exits {redExits}/{volumeExits}, bullish holds {increasingBullishHolds}, daily/session rejections {dailyRejected}/{sessionRejected}, entries before/after DST {preDstEntries}/{postDstEntries}.");
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null);
		IsTrue(entries > 0 && exits > 0 && redExits > 0 && volumeExits > 0 && increasingBullishHolds > 0 && dailyRejected > 0);
		IsTrue(preDstEntries > 0 && postDstEntries > 0);
		if (!secondary && !useSession)
			IsTrue(tierCounts.All(count => count > 0), "The fixture must exercise full, half and quarter entry sizing.");
		if (useSession)
			IsTrue(sessionRejected > 0);
	}
}
