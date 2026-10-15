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
	[TestCategory("Shard06")]
	[DataRow(14, 40, false)]
	[DataRow(5, 15, true)]
	public async Task S1166_PineconnectorReversesOnEveryMovingAverageCross(int fastLength, int slowLength, bool secondary)
	{
		const string key = "1166_Pineconnector_Template";
		var fast = new ImpulseEma(fastLength);
		var slow = new ImpulseEma(slowLength);
		decimal? previousDifference = null;
		DateTime? previousSignalTime = null;
		Sides? expectedSide = null;
		var expectedVolume = 0m;
		var expectedOrders = 0;
		var actualOrders = 0;
		var buys = 0;
		var sells = 0;
		var reversals = 0;
		var signalsWithinSixHours = 0;
		var violations = new List<string>();
		await Replay(key, (strategy, alternateSecurity) =>
		{
			if (secondary)
				strategy.Security = alternateSecurity;
			AreEqual(14, strategy.Parameters["FastLength"].Value);
			AreEqual(40, strategy.Parameters["SlowLength"].Value);
			AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
			SetParam(strategy, "FastLength", fastLength);
			SetParam(strategy, "SlowLength", slowLength);
			strategy.CandleReceived += (_, candle) =>
			{
				if (candle.State != CandleStates.Finished)
					return;
				expectedSide = null;
				fast.Add(candle.ClosePrice);
				slow.Add(candle.ClosePrice);
				if (!fast.Formed || !slow.Formed)
					return;
				var difference = fast.Value - slow.Value;
				var prior = previousDifference;
				previousDifference = difference;
				if (prior is null)
					return;
				if (prior <= 0m && difference > 0m && strategy.Position <= 0m)
				{
					expectedSide = Sides.Buy;
					buys++;
				}
				else if (prior >= 0m && difference < 0m && strategy.Position >= 0m)
				{
					expectedSide = Sides.Sell;
					sells++;
				}
				if (expectedSide is null)
					return;
				expectedVolume = strategy.Volume + Math.Abs(strategy.Position);
				expectedOrders++;
				if (strategy.Position != 0m)
					reversals++;
				if (previousSignalTime is { } previous && strategy.CurrentTime - previous < TimeSpan.FromHours(6))
					signalsWithinSixHours++;
				previousSignalTime = strategy.CurrentTime;
			};
			strategy.OrderRegistering += order =>
			{
				actualOrders++;
				if (order.Side != expectedSide || order.Volume != expectedVolume || order.Type != OrderTypes.Market)
					violations.Add($"{strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expectedSide} {expectedVolume}. Every formed moving-average cross must open or fully reverse the position.");
				expectedSide = null;
			};
		}, TimeSpan.FromDays(31));
		IsTrue(violations.Count == 0, string.Join(Environment.NewLine, violations.Take(12)));
		AreEqual(expectedOrders, actualOrders, "A hidden cooldown cannot suppress a documented crossover.");
		IsTrue(buys > 0 && sells > 0 && reversals > 0, "The archive must exercise both sides and full reversals.");
		IsTrue(signalsWithinSixHours > 0, "The archive must exercise opposite signals less than six hours apart.");
		TestContext.WriteLine($"{key}: {buys} buys, {sells} sells, {reversals} full reversals, {signalsWithinSixHours} signals less than six hours apart; {actualOrders} orders.");
	}
}
