namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Ecng.Common;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;

public abstract partial class StrategyTests
{
	[TestMethod]
	[TestCategory("Shard00")]
	[DataRow(40, 10, 3.0, false)]
	[DataRow(12, 7, 2.0, true)]
	public async Task S1168_SevenPercentileWindowsAndPriceSupertrendDetermineEveryOrder(int length, int atrPeriod, double multiplier, bool secondary)
	{
		const string key = "1168_Pivot_Percentile_Trend";
		var bars = new Queue<(decimal High, decimal Low)>();
		var count = 0;
		var atr = 0m;
		var previousClose = 0m;
		decimal? upper = null;
		decimal? lower = null;
		decimal? trendClose = null;
		bool? up = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var rejectedBySupertrend = 0;
		var neutral = 0;
		var longerWindowsChangeTheSignal = 0;
		var violations = new List<string>();
		await Replay(key, (strategy, alternateSecurity) =>
		{
			if (secondary)
				strategy.Security = alternateSecurity;
			AreEqual(40, strategy.Parameters["PercentileLength"].Value);
			IsTrue(strategy.Parameters.ContainsKey("AtrPeriod") && strategy.Parameters.ContainsKey("AtrMultiplier"),
				"The percentile strategy needs an actual configurable price Supertrend, not an EMA crossover proxy.");
			AreEqual(10, strategy.Parameters["AtrPeriod"].Value);
			AreEqual(3m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "PercentileLength", length);
			SetParam(strategy, "AtrPeriod", atrPeriod);
			SetParam(strategy, "AtrMultiplier", (decimal)multiplier);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				expectedSide = null;
				count++;
				bars.Enqueue((candle.HighPrice, candle.LowPrice));
				if (bars.Count > length * 7)
					bars.Dequeue();
				var range = count == 1 ? candle.HighPrice - candle.LowPrice
					: Math.Max(candle.HighPrice - candle.LowPrice,
						Math.Max(Math.Abs(candle.HighPrice - previousClose), Math.Abs(candle.LowPrice - previousClose)));
				var atrCount = Math.Min(count, atrPeriod);
				atr = (atr * (atrCount - 1) + range) / atrCount;
				previousClose = candle.ClosePrice;
				if (count < atrPeriod)
					return;
				var midpoint = (candle.HighPrice + candle.LowPrice) / 2m;
				var basicUpper = midpoint + (decimal)multiplier * atr;
				var basicLower = midpoint - (decimal)multiplier * atr;
				upper = upper is null || basicUpper < upper || trendClose > upper ? basicUpper : upper;
				lower = lower is null || basicLower > lower || trendClose < lower ? basicLower : lower;
				up = up is null ? candle.ClosePrice >= midpoint
					: up.Value ? candle.ClosePrice > lower : candle.ClosePrice >= upper;
				trendClose = candle.ClosePrice;
				if (bars.Count < length * 7)
					return;
				var line = up.Value ? lower.Value : upper.Value;
				var strength = 0;
				var firstWindowStrength = 0;
				for (var window = 1; window <= 7; window++)
				{
					var period = length * window;
					var observations = bars.TakeLast(period).ToArray();
					// Order-statistic inequalities determine the same strict quartile comparisons
					// without sorting or calling the strategy's percentile implementation.
					var bullish = observations.Count(bar => bar.High < candle.HighPrice) >= (3 * period + 3) / 4;
					var bearish = observations.Count(bar => bar.Low <= candle.LowPrice) < (period + 3) / 4;
					var vote = (bullish ? 1 : 0) - (bearish ? 1 : 0);
					strength += vote;
					if (window == 1)
						firstWindowStrength = vote;
				}
				if (Math.Sign(strength) != Math.Sign(firstWindowStrength))
					longerWindowsChangeTheSignal++;
				if (strength == 0)
					neutral++;
				else if (strength > 0 && candle.ClosePrice <= line || strength < 0 && candle.ClosePrice >= line)
					rejectedBySupertrend++;
				if (strength > 0 && candle.ClosePrice > line && strategy.Position <= 0m)
				{
					expectedSide = Sides.Buy;
					buys++;
				}
				else if (strength < 0 && candle.ClosePrice < line && strategy.Position >= 0m)
				{
					expectedSide = Sides.Sell;
					sells++;
				}
				if (expectedSide is not null)
				{
					expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
					expectedOrders++;
				}
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume}. Every order must combine seven percentile windows with the price Supertrend and fully reverse an opposing position.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(buys > 0 && sells > 0, "The archive must exercise both directions.");
		IsTrue(neutral > 0 && rejectedBySupertrend > 0, "Neutral strengths and the Supertrend filter must prevent real entries.");
		IsTrue(longerWindowsChangeTheSignal > 0, "The longer percentile windows must change the signal from the first window alone.");
		TestContext.WriteLine($"{key}: {buys} buys, {sells} sells, {neutral} neutral bars, {rejectedBySupertrend} bars rejected by Supertrend, {longerWindowsChangeTheSignal} signals changed by longer windows; {actualOrders} orders.");
	}
}
