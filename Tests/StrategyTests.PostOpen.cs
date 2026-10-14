namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	private const string PostOpen = "1175_Post_Open_Long_ATR_Stop_Loss_Take_Profit";

	/// <summary>ATR protection scenarios for the opening-breakout contract.</summary>
	public enum PostOpenRiskProfiles
	{
		/// <summary>Published stop-loss and take-profit multipliers.</summary>
		Published,
		/// <summary>Tight stop loss with a distant take profit.</summary>
		TightStop,
		/// <summary>Tight take profit with a distant stop loss.</summary>
		TightTake,
	}

	[TestMethod]
	[TestCategory("Shard07")]
	public async Task S1175_DefaultNeverOpensShortPositions()
	{
		var minimum = 0m;
		var position = 0m;
		await Replay(PostOpen, (strategy, _) =>
			strategy.OwnTradeReceived += (_, trade) =>
			{
				position += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				minimum = Math.Min(minimum, position);
			}, TimeSpan.FromDays(31));
		IsTrue(minimum >= 0m, "The published long-only strategy must never open a real short position.");
	}

	[TestMethod]
	[TestCategory("Shard07")]
	[DataRow(false, 570, 5, 1.0, PostOpenRiskProfiles.Published)]
	[DataRow(true, 960, 3, 0.8, PostOpenRiskProfiles.Published)]
	[DataRow(false, 1425, 3, 1.0, PostOpenRiskProfiles.Published)]
	[DataRow(false, 570, 5, 1.0, PostOpenRiskProfiles.TightStop)]
	[DataRow(false, 570, 5, 1.0, PostOpenRiskProfiles.TightTake)]
	public async Task S1175_OpeningBreakoutsAndFrozenAtrProtectionDetermineEveryOrder(bool secondary,
		int startMinutes, int resistancePeriod, double proximity, PostOpenRiskProfiles riskProfile)
	{
		var bbPeriod = 14;
		var bbMultiplier = secondary ? 2m : 1.5m;
		var fastPeriod = secondary ? 5 : 10;
		var slowPeriod = secondary ? 80 : 200;
		var rsiPeriod = secondary ? 4 : 7;
		var rsiThreshold = secondary ? 60m : 30m;
		var adxPeriod = secondary ? 10 : 7;
		var adxThreshold = secondary ? 25m : 10m;
		var atrPeriod = secondary ? 7 : 14;
		var stopMultiplier = riskProfile == PostOpenRiskProfiles.TightStop ? 0.05m : riskProfile == PostOpenRiskProfiles.TightTake ? 100m : secondary ? 1.5m : 2m;
		var takeMultiplier = riskProfile == PostOpenRiskProfiles.TightTake ? 0.05m : riskProfile == PostOpenRiskProfiles.TightStop ? 100m : secondary ? 3m : 4m;
		var start = TimeSpan.FromMinutes(startMinutes);
		var duration = TimeSpan.FromHours(1);
		var fast = new ImpulseEma(fastPeriod);
		var slow = new ImpulseEma(slowPeriod);
		var rsi = new RelativeStrengthIndex { Length = rsiPeriod };
		var adx = new AverageDirectionalIndex { Length = adxPeriod };
		var closes = new Queue<decimal>();
		var highs = new Queue<decimal>();
		var violations = new List<string>();
		var count = 0;
		var atr = 0m;
		decimal? previousClose = null;
		var eligibleEntry = false;
		var expectedEntries = 0;
		var entries = 0;
		var exits = 0;
		var stopExits = 0;
		var takeExits = 0;
		var intrabarExits = 0;
		var frozenFills = 0;
		var sessionRejects = 0;
		var basisRejects = 0;
		var rsiRejects = 0;
		var adxRejects = 0;
		var trendRejects = 0;
		var bearishRejects = 0;
		var openingBars = 0;
		var closingBars = 0;
		var filledPosition = 0m;
		var fillValue = 0m;
		var fillPrice = 0m;
		var frozenStop = 0m;
		var frozenTake = 0m;
		var protectionPrice = 0m;
		var entryBar = 0;
		DateTime? entryFillTime = null;
		Order pending = null;
		var stopField = typeof(Strategy).GetField("_stopLoss", BindingFlags.Instance | BindingFlags.NonPublic);
		var takeField = typeof(Strategy).GetField("_takeProfit", BindingFlags.Instance | BindingFlags.NonPublic);
		await Replay(PostOpen, (strategy, alternateSecurity) =>
		{
			if (secondary)
				strategy.Security = alternateSecurity;
			foreach (var (name, value) in new (string, object)[]
			{
				("BbLength", 14), ("BbMult", 1.5m), ("EmaLength", 10), ("EmaLongLength", 200),
				("RsiLength", 7), ("RsiThreshold", 30m), ("AdxLength", 7), ("AdxThreshold", 10m),
				("AtrLength", 14), ("AtrStopLossMultiplier", 2m), ("AtrTakeProfitMultiplier", 4m),
				("SessionStart", TimeSpan.FromMinutes(570)), ("SessionDuration", TimeSpan.FromHours(1)),
				("ResistancePeriod", 5), ("BasisProximity", 1m),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"The opening-breakout README requires {name}.");
				AreEqual(value, strategy.Parameters[name].Value, $"Published default of {name}.");
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			foreach (var (name, value) in new (string, object)[]
			{
				("BbMult", bbMultiplier), ("EmaLength", fastPeriod), ("EmaLongLength", slowPeriod),
				("RsiLength", rsiPeriod), ("RsiThreshold", rsiThreshold), ("AdxLength", adxPeriod),
				("AdxThreshold", adxThreshold), ("AtrLength", atrPeriod),
				("AtrStopLossMultiplier", stopMultiplier), ("AtrTakeProfitMultiplier", takeMultiplier),
				("SessionStart", start), ("SessionDuration", duration),
				("ResistancePeriod", resistancePeriod), ("BasisProximity", (decimal)proximity),
			})
				SetParam(strategy, name, value);
			strategy.Engine.CurrentPriceUpdated += (securityId, price, _, _) =>
			{
				if (securityId == strategy.Security.Id.ToSecurityId())
					protectionPrice = price;
			};
			strategy.Level1Received += (_, quote) =>
			{
				if (quote.TryGetDecimal(Level1Fields.BestBidPrice) is decimal price && price > 0m)
					protectionPrice = price;
			};
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (eligibleEntry)
					violations.Add($"{candle.OpenTime:O}: a qualified opening breakout did not submit its entry.");
				eligibleEntry = false;
				count++;
				var trueRange = previousClose is decimal previous
					? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - previous), Math.Abs(candle.LowPrice - previous)))
					: candle.HighPrice - candle.LowPrice;
				var atrCount = Math.Min(count, atrPeriod);
				atr = (atr * (atrCount - 1) + trueRange) / atrCount;
				previousClose = candle.ClosePrice;
				var resistance = highs.Count == resistancePeriod ? highs.Max() : (decimal?)null;
				highs.Enqueue(candle.HighPrice);
				if (highs.Count > resistancePeriod)
					highs.Dequeue();
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > bbPeriod)
					closes.Dequeue();
				fast.Add(candle.ClosePrice);
				slow.Add(candle.ClosePrice);
				var rsiValue = rsi.Process(candle);
				var adxValue = (AverageDirectionalIndexValue)adx.Process(candle);
				if (!fast.Formed || !slow.Formed || !rsiValue.IsFormed || !adxValue.IsFormed
					|| adxValue.MovingAverage is not decimal strength || count < atrPeriod || closes.Count < bbPeriod || resistance is null)
					return;
				var mean = closes.Average();
				var deviation = (decimal)Math.Sqrt((double)closes.Average(price => (price - mean) * (price - mean)));
				var age = (candle.OpenTime.TimeOfDay - start + TimeSpan.FromDays(1)).Ticks % TimeSpan.TicksPerDay;
				var withinSession = age < duration.Ticks;
				if (age == 0)
					openingBars++;
				if (age == duration.Ticks)
					closingBars++;
				var breakout = candle.ClosePrice > resistance;
				var nearBasis = Math.Abs(candle.ClosePrice - mean) <= bbMultiplier * deviation * (decimal)proximity;
				var rsiPass = rsiValue.GetValue<decimal>() > rsiThreshold;
				var adxPass = strength > adxThreshold;
				var trendPass = fast.Value > slow.Value && candle.ClosePrice > fast.Value;
				var noPullback = candle.ClosePrice >= candle.OpenPrice;
				if (breakout)
				{
					if (!withinSession)
						sessionRejects++;
					if (!nearBasis)
						basisRejects++;
					if (!rsiPass)
						rsiRejects++;
					if (!adxPass)
						adxRejects++;
					if (!trendPass)
						trendRejects++;
					if (!noPullback)
						bearishRejects++;
				}
				eligibleEntry = strategy.Position == 0m && (pending is null || pending.State is OrderStates.Done or OrderStates.Failed)
					&& withinSession && breakout && nearBasis && rsiPass && adxPass && trendPass && noPullback && atr > 0m;
				if (eligibleEntry)
					expectedEntries++;
			};
			strategy.OrderRegistering += order =>
			{
				pending = order;
				if (order.Type != OrderTypes.Market || strategy.Position != filledPosition)
					violations.Add("Orders must be market orders against the independently accumulated actual position.");
				if (order.Side == Sides.Buy)
				{
					entries++;
					if (!eligibleEntry || order.Volume != strategy.Volume || strategy.Position != 0m)
						violations.Add($"{strategy.CurrentTime:O}: buy requires a flat, qualified opening breakout through every published filter.");
					frozenStop = atr * stopMultiplier;
					frozenTake = atr * takeMultiplier;
					eligibleEntry = false;
				}
				else
				{
					exits++;
					var stop = fillPrice - frozenStop;
					var take = fillPrice + frozenTake;
					var isStop = protectionPrice > 0m && protectionPrice <= stop;
					var isTake = protectionPrice >= take;
					if (strategy.Position <= 0m || order.Volume != strategy.Position || !(isStop || isTake))
						violations.Add($"{strategy.CurrentTime:O}: sell {order.Volume} must fully close a long at actual-fill ATR stop/take; fill={fillPrice}, SL={stop}, TP={take}, trigger={protectionPrice}.");
					if (isStop)
						stopExits++;
					if (isTake)
						takeExits++;
					if (entryFillTime is DateTime filledAt && count == entryBar && strategy.CurrentTime < filledAt.AddMinutes(4))
						intrabarExits++;
				}
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				var wasFlat = filledPosition == 0m;
				filledPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				if (filledPosition < 0m)
					violations.Add("The strategy cannot open short positions.");
				if (trade.Order.Side == Sides.Buy)
				{
					fillValue += trade.Trade.Price * trade.Trade.Volume;
					fillPrice = fillValue / filledPosition;
					if (!wasFlat)
						fillPrice = strategy.Security.ShrinkPrice(fillPrice);
					var stop = (Unit)stopField.GetValue(strategy);
					var take = (Unit)takeField.GetValue(strategy);
					if (stop is null || take is null || stop.Type != UnitTypes.Absolute || take.Type != UnitTypes.Absolute
						|| Math.Abs(stop.Value - frozenStop) > 0.00000001m || Math.Abs(take.Value - frozenTake) > 0.00000001m)
						violations.Add("Every actual entry fill needs native SL and TP Units frozen from that signal's independently calculated Wilder ATR.");
					frozenFills++;
					entryFillTime = strategy.CurrentTime;
					entryBar = count;
				}
				else if (filledPosition == 0m)
				{
					fillValue = fillPrice = 0m;
				}
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(!eligibleEntry);
		AreEqual(expectedEntries, entries);
		IsTrue(entries > 0 && exits > 0 && frozenFills > 0);
		IsTrue(openingBars > 0 && closingBars > 0 && sessionRejects > 0 && basisRejects > 0 && trendRejects > 0);
		AreEqual(exits, stopExits + takeExits, "The latest protection price must unambiguously explain each exit.");
		if (riskProfile == PostOpenRiskProfiles.TightStop)
			IsTrue(stopExits > 0 && intrabarExits > 0, "The tight ATR stop must really close between finished signal candles.");
		if (riskProfile == PostOpenRiskProfiles.TightTake)
			IsTrue(takeExits > 0 && intrabarExits > 0, "The tight ATR take profit must really close between finished signal candles.");
		TestContext.WriteLine($"{PostOpen}: {entries} entries, {exits} exits ({stopExits} stops/{takeExits} takes), {frozenFills} frozen-ATR fills, {intrabarExits} intrabar exits; rejects: session={sessionRejects}, basis={basisRejects}, RSI={rsiRejects}, ADX={adxRejects}, EMA={trendRejects}, bearish={bearishRejects}; opening/closing boundary bars={openingBars}/{closingBars}.");
	}
}
