namespace StockSharp.Tests;

using System;
using System.IO;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo.Compilation;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>Behavioral checks reserved for Point 1 / Part 01. Both language runners inherit these tests.</summary>
public abstract partial class StrategyTests
{
    private const string Point1Part01KeltnerKey = "0333_Keltner_Seasonal_Filter";

    // Independent expected calendar from the published strategy contract.
    private static readonly decimal[] Point1Part01KeltnerCalendar =
        [0m, 0.8m, 0.3m, 0.6m, 0.7m, 0.2m, -0.3m, -0.1m, -0.4m, -0.8m, 0.1m, 0.9m, 0.7m];

    [TestMethod]
    [TestCategory("Shard05")]
    public async Task Point1Part01_0333_DefaultCalendarGatesFilledEntries()
    {
        var flatEntries = 0;
        var forbiddenCandleMonths = 0;
        await Replay(Point1Part01KeltnerKey, (strategy, _) =>
        {
            AreEqual(20, Convert.ToInt32(strategy.Parameters["EmaPeriod"].Value));
            AreEqual(14, Convert.ToInt32(strategy.Parameters["AtrPeriod"].Value));
            AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
            AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["SeasonalThreshold"].Value));
            AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);

            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State == CandleStates.Finished
                    && Math.Abs(Point1Part01KeltnerCalendar[candle.OpenTime.Month]) <= 0.5m)
                    forbiddenCandleMonths++;
            };
            strategy.OrderRegistering += order =>
            {
                // From flat, a market order must be an entry, never a position exit.
                if (strategy.Position != 0m)
                    return;

                var seasonalScore = Point1Part01KeltnerCalendar[strategy.CurrentTime.Month];
                if (order.Side == Sides.Buy)
                    IsTrue(seasonalScore > 0.5m, "A new long needs a strictly positive seasonal score above the published threshold.");
                else if (order.Side == Sides.Sell)
                    IsTrue(seasonalScore < -0.5m, "A new short needs a strictly negative seasonal score below the published threshold.");
                else
                    Fail("A market entry must have an explicit buy or sell side.");
                flatEntries++;
            };
        });
        IsTrue(flatEntries > 0, "The default replay must actually exercise the seasonal entry gate with real orders and fills.");
        IsTrue(forbiddenCandleMonths > 0, "The same historical replay must cover at least one forbidden month.");
    }

    [TestMethod]
    [TestCategory("Shard05")]
    [DoNotParallelize]
    public async Task Point1Part01_0333_ReplayIgnoresPythonStartedMonth()
    {
        // The existing compiled-C# probe checks January against September. This test
        // adds the analogous Python source replay without changing production code.
        if (!IsPython)
        {
            await Replay(Point1Part01KeltnerKey, null);
            return;
        }

        async Task<OrderTraceRecorder> ReplayWithStartedMonth(int month)
        {
            var path = Path.Combine("../../../../API/", StrategyInventory.GetFile(Point1Part01KeltnerKey, ".py"));
            var original = File.ReadAllText(path);
            const string originalBaseCall = "super(keltner_seasonal_strategy, self).OnStarted2(time)";
            IsTrue(original.Contains(originalBaseCall, StringComparison.Ordinal), "The Python startup hook changed; inspect this probe.");

            // Keep every candle, parameter and signal identical; vary only the DateTime
            // passed to the strategy's base OnStarted2, as in the existing C# probe.
            var modified = original
                .Replace("from System import TimeSpan, Math", "from System import TimeSpan, Math, DateTime", StringComparison.Ordinal)
                .Replace(originalBaseCall,
                    $"super(keltner_seasonal_strategy, self).OnStarted2(DateTime(time.Year, {month}, 1, time.Hour, time.Minute, time.Second, time.Kind))",
                    StringComparison.Ordinal);

            var code = new CodeInfo
            {
                Name = "keltner_seasonal_strategy",
                Text = modified,
                Language = FileExts.Python,
            };
            var errors = await code.CompileAsync(t => t.IsRequiredType<Strategy>(), code.Name, CancellationToken);
            foreach (var error in errors.ErrorsOnly())
                throw new InvalidOperationException(error.ToString());

            var strategy = code.ObjectType.CreateInstance<Strategy>();
            var trace = new OrderTraceRecorder();
            await AsmInit.RunStrategy(strategy, CancellationToken, (s, _) => trace.Attach(s));
            return trace;
        }

        var january = await ReplayWithStartedMonth(1);
        var september = await ReplayWithStartedMonth(9);
        january.AssertSameAs(september);
    }
}
