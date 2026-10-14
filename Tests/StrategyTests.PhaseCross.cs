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
	[TestCategory("Shard04")]
	public async Task S1164_PhaseCrossOnlyOpensLongAndClosesAtTheOppositeCross()
	{
		var ignoredDownCrosses = await CheckPhaseCrossArchive(20, 0.5m, false);
		ignoredDownCrosses += await CheckPhaseCrossArchive(8, 0.01m, true);
		IsTrue(ignoredDownCrosses > 0, "The archive must exercise a downward cross while flat, which cannot open a short.");
	}

	private async Task<int> CheckPhaseCrossArchive(int length, decimal offset, bool secondary)
	{
		const string key = "1164_Phase_Cross_with_Zone";
		var closes = new Queue<decimal>();
		var emaLength = checked(length * 2);
		var emaSeed = new Queue<decimal>();
		var ema = 0m;
		var alpha = 2m / (emaLength + 1);
		var shift = offset;
		decimal? previousDifference = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var longEntries = 0;
		var exits = 0;
		var ignoredDownCrosses = 0;
		var filledPosition = 0m;
		var violations = new List<string>();
		await Replay(key, (strategy, alternateSecurity) =>
		{
			if (secondary)
				strategy.Security = alternateSecurity;
			AreEqual(20, strategy.Parameters["Length"].Value);
			AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["Offset"].Value));
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "Length", length);
			SetParam(strategy, "Offset", shift);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				expectedSide = null;
				closes.Enqueue(candle.ClosePrice);
				if (closes.Count > length)
					closes.Dequeue();
				if (emaSeed.Count < emaLength)
				{
					emaSeed.Enqueue(candle.ClosePrice);
					if (emaSeed.Count < emaLength)
						return;
					ema = emaSeed.Sum() / emaLength;
				}
				else
					ema = (candle.ClosePrice - ema) * alpha + ema;
				var difference = closes.Sum() / length + shift - (ema - shift);
				var prior = previousDifference;
				previousDifference = difference;
				if (prior is null)
					return;
				if (prior <= 0m && difference > 0m && strategy.Position == 0m)
				{
					expectedSide = Sides.Buy;
					expectedVolume = strategy.Volume;
					longEntries++;
				}
				else if (prior >= 0m && difference < 0m)
				{
					if (strategy.Position > 0m)
					{
						expectedSide = Sides.Sell;
						expectedVolume = strategy.Position;
						exits++;
					}
					else if (strategy.Position == 0m)
						ignoredDownCrosses++;
				}
				if (expectedSide is not null)
					expectedOrders++;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume}. Phase Cross may only enter long while flat and close that long on the opposite cross.");
				expectedSide = null;
			};
			strategy.OwnTradeReceived += (_, trade) =>
			{
				if (trade?.Order is null || trade.Trade is null)
					return;
				filledPosition += trade.Order.Side == Sides.Buy ? trade.Trade.Volume : -trade.Trade.Volume;
				if (filledPosition < 0m)
					violations.Add($"{trade.Trade.ServerTime:O}: a fill opened a short position of {filledPosition}.");
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders);
		IsTrue(longEntries > 0 && exits > 0, "The archive must exercise long entries and their complete exits.");
		TestContext.WriteLine($"{key}: {longEntries} long entries, {exits} exits, {ignoredDownCrosses} downward crosses ignored while flat; {actualOrders} orders.");
		return ignoredDownCrosses;
	}
}
