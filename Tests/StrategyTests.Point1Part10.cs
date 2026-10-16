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
    public async Task S3849_EntriesRequireActualOpeningGapsAndPublishedVolume()
    {
        var orders = 0;
        var fills = 0;
        var invalid = new List<string>();
        decimal? previousHigh = null;
        decimal? previousLow = null;
        DateTimeOffset? observedBar = null;
        Sides? necessarySide = null;
        var ordersThisBar = 0;
        var point = 0m;

        await Replay(Point1Part10GapKey, (strategy, _) =>
        {
            AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
            AreEqual(1m, Convert.ToDecimal(strategy.Parameters["MinGapSize"].Value));
            AreEqual(0.1m, Convert.ToDecimal(strategy.Parameters["GapVolume"].Value));
            IsFalse(strategy.Parameters.ContainsKey("EmaPeriod"), "The gap algorithm has no EMA filter.");
            point = strategy.Security.PriceStep ?? 0m;
            IsTrue(point > 0m);

            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State is not (CandleStates.Active or CandleStates.Finished))
                    return;
                if (observedBar != candle.OpenTime)
                {
                    observedBar = candle.OpenTime;
                    ordersThisBar = 0;
                    necessarySide = null;
                    if (previousHigh is decimal high && previousLow is decimal low)
                    {
                        // A real quoted spread can only widen the mandatory two-step
                        // minimum buffer, so this remains a valid independent oracle.
                        if (candle.OpenPrice > high + 2m * point)
                            necessarySide = Sides.Sell;
                        else if (candle.OpenPrice < low - 2m * point)
                            necessarySide = Sides.Buy;
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
                ordersThisBar++;
                if (order.Type != OrderTypes.Market || order.Volume != 0.1m)
                    invalid.Add($"Expected a market order of 0.1 lots, got {order.Type} {order.Volume}.");
                if (necessarySide is null || order.Side != necessarySide)
                    invalid.Add($"Entry {order.Side} without a qualifying previous-candle opening gap for {observedBar}.");
                if (ordersThisBar > 1)
                    invalid.Add($"Duplicate order in a single candle {observedBar}.");
            };
            strategy.OwnTradeReceived += (_, trade) =>
            {
                if (trade.Trade is not null)
                    fills++;
            };
        }, TimeSpan.FromDays(31));

        AreEqual(0, invalid.Count, string.Join(Environment.NewLine, invalid.Take(12)));
        IsTrue(orders > 0 && fills > 0, "The archive must exercise real orders and actual fills.");
    }
}
