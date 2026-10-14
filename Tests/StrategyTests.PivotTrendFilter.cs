namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Configuration;
using StockSharp.Messages;

public abstract partial class StrategyTests
{
	[TestMethod]
	[TestCategory("Shard01")]
	[DataRow(false, 2, 3.0, 10, 10, 3.0, 20, false)]
	[DataRow(true, 4, 2.0, 7, 14, 2.5, 50, false)]
	[DataRow(false, 2, 3.0, 10, 10, 3.0, 20, true)]
	public async Task S1169_PivotsTrendFilterSmaAndDatesDetermineEveryOrder(bool secondary, int pivotPeriod,
		double factor, int atrPeriod, int trendPeriod, double trendMultiplier, int maPeriod, bool boundaryWindow)
	{
		const string key = "1169_Pivot_Point_SuperTrend_TrendFilter";
		var candles = new List<(decimal High, decimal Low, decimal Close)>();
		var violations = new List<string>();
		var ranges = new List<decimal>();
		var trendAtr = 0m;
		var pivotReference = new PivotBandsReference(pivotPeriod, atrPeriod, (decimal)factor);
		decimal? filterSupport = null;
		decimal? filterResistance = null;
		bool? filterUp = null;
		var start = secondary ? Paths.HistoryBeginDate.AddDays(8) : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
		var end = secondary ? Paths.HistoryBeginDate.AddDays(16) : DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);
		if (boundaryWindow)
			start = end = Paths.HistoryBeginDate.AddMinutes(110);
		var boundaryEntries = 0;
		var boundaryPivotSignals = 0;
		var pivotOnlyEntries = 0;
		var filterOnlyEntries = 0;
		var smaRejections = 0;
		var datedRejections = 0;
		var conflicts = 0;
		var closes = 0;
		var reversals = 0;
		var buys = 0;
		var sells = 0;
		var outsideWindowOrders = 0;
		var expectedOrders = 0;
		var actualOrders = 0;
		var filledPosition = 0m;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		await Replay(key, (strategy, alternateSecurity) =>
		{
			if (secondary)
				strategy.Security = alternateSecurity;
			foreach (var (name, value) in new (string, object)[]
			{
				("PivotPeriod", 2), ("Factor", 3m), ("AtrPeriod", 10), ("TrendAtrPeriod", 10),
				("TrendMultiplier", 3m), ("MaPeriod", 20),
				("StartDate", DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc)),
				("EndDate", DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)),
			})
			{
				IsTrue(strategy.Parameters.ContainsKey(name), $"The README requires {name}; an EMA crossover is not the documented model.");
				AreEqual(value, strategy.Parameters[name].Value, $"Published default of {name}.");
			}
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "PivotPeriod", pivotPeriod);
			SetParam(strategy, "Factor", (decimal)factor);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "TrendAtrPeriod", trendPeriod);
			SetParam(strategy, "TrendMultiplier", (decimal)trendMultiplier);
			SetParam(strategy, "MaPeriod", maPeriod);
			SetParam(strategy, "StartDate", start);
			SetParam(strategy, "EndDate", end);
			strategy.OwnTradeReceived += (_, trade) => filledPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				if (expectedSide is not null)
					violations.Add($"{candle.OpenTime:O}: the previous signal did not register its order.");
				expectedSide = null;
				var previousClose = candles.Count == 0 ? candle.ClosePrice : candles[^1].Close;
				candles.Add((candle.HighPrice, candle.LowPrice, candle.ClosePrice));
				ranges.Add(Math.Max(candle.HighPrice - candle.LowPrice,
					Math.Max(Math.Abs(candle.HighPrice - previousClose), Math.Abs(candle.LowPrice - previousClose))));
				var count = candles.Count;
				// The seed is a growing average; subsequent values use Wilder's recurrence.
				trendAtr = count <= trendPeriod ? ranges.Average() : trendAtr + (ranges[^1] - trendAtr) / trendPeriod;
				var previousFilterUp = filterUp;
				if (count >= trendPeriod)
				{
					var midpoint = (candle.HighPrice + candle.LowPrice) / 2m;
					var upper = midpoint + (decimal)trendMultiplier * trendAtr;
					var lower = midpoint - (decimal)trendMultiplier * trendAtr;
					filterResistance = filterResistance is null || upper < filterResistance || previousClose > filterResistance ? upper : filterResistance;
					filterSupport = filterSupport is null || lower > filterSupport || previousClose < filterSupport ? lower : filterSupport;
					filterUp = filterUp is null ? candle.ClosePrice >= midpoint
						: filterUp.Value ? candle.ClosePrice > filterSupport : candle.ClosePrice >= filterResistance;
				}
				var pivotSignal = pivotReference.Add(candle);
				if (count < maPeriod || pivotReference.IsUp is null || filterUp is null)
					return;
				var pivotBuy = pivotSignal == Sides.Buy;
				var pivotSell = pivotSignal == Sides.Sell;
				var filterBuy = previousFilterUp == false && filterUp == true;
				var filterSell = previousFilterUp == true && filterUp == false;
				var average = candles.TakeLast(maPeriod).Average(bar => bar.Close);
				var withinDates = candle.OpenTime >= start && candle.OpenTime <= end;
				if (boundaryWindow && withinDates && (pivotBuy || pivotSell) && !filterBuy && !filterSell)
					boundaryPivotSignals++;
				var upSignal = pivotBuy || filterBuy;
				var downSignal = pivotSell || filterSell;
				var enterLong = !downSignal && (filterBuy && candle.ClosePrice > average || pivotBuy && withinDates);
				var enterShort = !upSignal && (filterSell && candle.ClosePrice < average || pivotSell && withinDates);
				if (filterBuy && candle.ClosePrice <= average || filterSell && candle.ClosePrice >= average)
					smaRejections++;
				if ((pivotBuy || pivotSell) && !withinDates)
					datedRejections++;
				if (upSignal && downSignal)
					conflicts++;
				if (strategy.Position > 0m && downSignal)
				{
					expectedSide = Sides.Sell;
					expectedVolume = strategy.Position + (enterShort ? strategy.Volume : 0m);
				}
				else if (strategy.Position < 0m && upSignal)
				{
					expectedSide = Sides.Buy;
					expectedVolume = -strategy.Position + (enterLong ? strategy.Volume : 0m);
				}
				else if (strategy.Position == 0m && (enterLong || enterShort))
				{
					expectedSide = enterLong ? Sides.Buy : Sides.Sell;
					expectedVolume = strategy.Volume;
				}
				if (expectedSide is null)
					return;
				expectedOrders++;
				if (expectedSide == Sides.Buy)
					buys++;
				else
					sells++;
				var entering = expectedSide == Sides.Buy ? enterLong : enterShort;
				if (entering)
				{
					if (!filterBuy && !filterSell)
						pivotOnlyEntries++;
					if (!(pivotBuy && withinDates || pivotSell && withinDates))
						filterOnlyEntries++;
					if (boundaryWindow && withinDates)
						boundaryEntries++;
					if (strategy.Position != 0m)
						reversals++;
				}
				else
					closes++;
				if (!withinDates)
					outsideWindowOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (strategy.Position != filledPosition || order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume}, filled position {filledPosition}.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		IsTrue(expectedSide is null, "The final eligible signal must also register its order.");
		AreEqual(expectedOrders, actualOrders);
		IsTrue(pivotReference.Highs > 0 && pivotReference.Lows > 0 && buys > 0 && sells > 0);
		IsTrue(filterOnlyEntries > 0 && reversals > 0 && closes > 0, "The archive must exercise confirmed entries, full reversals and rejected-entry closes.");
		if (secondary || boundaryWindow)
			IsTrue(datedRejections > 0 && outsideWindowOrders > 0, "The date window gates pivot entries, not trend entries or exits.");
		else
			IsTrue(pivotOnlyEntries > 0 && smaRejections > 0, "Both independent entry paths and the SMA filter must affect trading.");
		if (boundaryWindow)
		{
			AreEqual(1, boundaryPivotSignals, "The chosen archive candle must carry an isolated pivot flip at both date endpoints.");
			AreEqual(1, boundaryEntries, "A pivot signal at StartDate == EndDate must enter exactly once.");
		}
		TestContext.WriteLine($"{key}: {actualOrders} orders ({buys} buys/{sells} sells), {reversals} reversals, {closes} closes, {pivotOnlyEntries} pivot-only/{filterOnlyEntries} filter-only entries, {smaRejections} SMA rejects, {datedRejections} date rejects, {conflicts} conflicts, {boundaryEntries} boundary entries, {outsideWindowOrders} orders outside dates; {pivotReference.Highs}/{pivotReference.Lows} confirmed pivots.");
	}
}
