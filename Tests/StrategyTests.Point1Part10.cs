namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;

public abstract partial class StrategyTests
{
    private const string Point1Part10GapKey = "3849_Gaps_Reversion";

    [TestMethod]
    [TestCategory("Shard01")]
    public async Task S3849_GapReversionRequiresRealGapAndPublishedParameters()
    {
        var orders = 0;
        var invalid = new List<string>();
        decimal? previousHigh = null;
        decimal? previousLow = null;
        DateTimeOffset? observedBar = null;
        var point = 0m;
        await Replay(Point1Part10GapKey, (strategy, _) =>
        {
            AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
            AreEqual(1m, Convert.ToDecimal(strategy.Parameters["MinGapSize"].Value));
            AreEqual(0.1m, Convert.ToDecimal(strategy.Parameters["GapVolume"].Value));
            IsFalse(strategy.Parameters.ContainsKey("EmaPeriod"), "The published strategy has no EMA filter.");
            point = strategy.Security.PriceStep ?? 0m;
            IsTrue(point > 0m);

            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State != CandleStates.Active && candle.State != CandleStates.Finished)
                    return;
                if (observedBar != candle.OpenTime)
                {
                    observedBar = candle.OpenTime;
                    // The spread can only increase the entry buffer, never shrink it
                    // below the README's (MinGapSize + one-step fallback) minimum.
                    if (previousHigh is decimal high && previousLow is decimal low)
                    {
                        var open = candle.OpenPrice;
                        if (open <= high + 2m * point && open >= low - 2m * point)
                            invalid.Add("No trade is allowed from an opening inside the previous range and its minimum spread buffer.");
                    }
                }
                if (candle.State == CandleStates.Finished)
                {
                    previousHigh = candle.HighPrice;
                    previousLow = candle.LowPrice;
                }
            };
            strategy.OrderRegistering += order =>
            {
                orders++;
                if (order.Type != OrderTypes.Market || order.Volume != 0.1m)
                    invalid.Add($"Order must be market and exactly 0.1 lots, got {order.Type} {order.Volume}.");
                if (observedBar is null || previousHigh is null || previousLow is null)
                    invalid.Add("An entry was made before the previous closed candle exists.");
                else
                {
                    if (order.Side == Sides.Buy && observedBar == null)
                        invalid.Add("Missing observed candle for buy.");
                }
            };
        }, TimeSpan.FromDays(31));

        IsTrue(orders > 0, "Require real executed opportunities in the packaged archive.");
        AreEqual(0, invalid.Count, string.Join(Environment.NewLine, invalid.Take(12)));
    }
}
