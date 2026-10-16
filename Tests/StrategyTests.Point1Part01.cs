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
using StockSharp.Samples.Strategies;
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
    public async Task Point1Part01_0333_DefaultMarchCalendarGatesFilledEntries()
    {
        var flatEntries = 0;
        var observedHistoricalCandles = 0;
        await Replay(Point1Part01KeltnerKey, (strategy, _) =>
        {
            AreEqual(20, Convert.ToInt32(strategy.Parameters["EmaPeriod"].Value));
            AreEqual(14, Convert.ToInt32(strategy.Parameters["AtrPeriod"].Value));
            AreEqual(2m, Convert.ToDecimal(strategy.Parameters["AtrMultiplier"].Value));
            AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["SeasonalThreshold"].Value));
            AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);

            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State == CandleStates.Finished)
                {
                    // The packaged BTC/TON fixture covers March 2024 only.
                    AreEqual(3, candle.OpenTime.Month);
                    observedHistoricalCandles++;
                }
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
        IsTrue(observedHistoricalCandles > 0, "The published-length indicators must be exercised on actual finished March candles.");
        // Other months cannot be claimed tested by this March-only fixture.
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

            // Keep every historical candle, published parameter and signal
            // identical. Change the actual DateTime received by the Python
            // strategy's OnStarted2, not merely the base.OnStarted2 call:
            // a future use of the argument in strategy code must be caught.
            const string originalHook = "    def OnStarted2(self, time):";
            IsTrue(original.Contains(originalHook, StringComparison.Ordinal), "The Python startup hook changed; inspect this probe.");
            var modified = original
                .Replace("from System import TimeSpan, Math", "from System import TimeSpan, Math, DateTime", StringComparison.Ordinal)
                .Replace(originalHook,
                    originalHook + $"\\n        time = DateTime(time.Year, {month}, 1, time.Hour, time.Minute, time.Second, time.Kind)",
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
    // Tests are required to fail against the pre-fix sources because all three
    // examples publish a real two-percent stop yet previously registered none.
    // Real entry and intrabar exit fills are checked by the shared helper.
    [TestMethod]
    [TestCategory("Shard07")]
    public Task Point1Part01_0103_DarkPoolPublishedStopExecutes()
        => CheckPercentStopBetweenBars("0103_Dark_Pool_Prints",
            duration: TimeSpan.FromDays(31), expectedFrame: TimeSpan.FromMinutes(1));

    [TestMethod]
    [TestCategory("Shard01")]
    public Task Point1Part01_0121_SantaClausPublishedStopExecutes()
        => CheckPercentStopBetweenBars("0121_Santa_Claus_Rally",
            duration: TimeSpan.FromDays(31));

    [TestMethod]
    [TestCategory("Shard02")]
    public Task Point1Part01_0122_JanuaryEffectPublishedStopExecutes()
        => CheckPercentStopBetweenBars("0122_January_Effect",
            duration: TimeSpan.FromDays(31));

    [TestMethod]
    [TestCategory("Shard07")]
    public async Task Point1Part01_0303_PublishedMarchSeasonalityForbidsNewShorts()
    {
        // The packaged fixture spans March 2024. March's published seasonal
        // score equals +0.5, so short entries are forbidden at threshold 0.5.
        // This probes REAL order registration and fills in both languages.
        var entries = 0;
        var fills = 0;
        await Replay("0303_Seasonality_Adjusted_Momentum", (strategy, _) =>
        {
            AreEqual(14, Convert.ToInt32(strategy.Parameters["MomentumPeriod"].Value));
            AreEqual(0.5m, Convert.ToDecimal(strategy.Parameters["SeasonalityThreshold"].Value));
            AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StopLossPercent"].Value));
            AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
            strategy.OrderRegistering += order =>
            {
                if (strategy.Position != 0m)
                    return;
                AreEqual(Sides.Buy, order.Side, "March +0.5 must never initiate a short at the original threshold.");
                entries++;
            };
            strategy.Trades.TradeAdded += trade =>
            {
                if (trade.Order.Side == Sides.Buy && strategy.Position > 0m)
                    fills++;
            };
        }, replayDuration: TimeSpan.FromDays(31));
        IsTrue(entries > 0 && fills > 0,
            "The unchanged default must produce at least one real March long entry and fill.");
    }

    [TestMethod]
    [TestCategory("Shard05")]
    public void Point1Part01_0333_OriginalSeasonalBreakoutConditions()
    {
        // Independent table from the published contract; no altered parameter
        // profile, no fabricated candles and no second implementation as oracle.
        const decimal ema = 100m;
        const decimal atr = 2m;
        const decimal multiplier = 2m;
        const decimal threshold = 0.5m;
        AreEqual(1, KeltnerSeasonalStrategy.GetEntrySignal(0.6m, 105m, ema, atr, multiplier, threshold, 0m),
            "March: Close > EMA + 2 ATR and strength +0.6 must permit the long.");
        AreEqual(0, KeltnerSeasonalStrategy.GetEntrySignal(0.3m, 105m, ema, atr, multiplier, threshold, 0m),
            "February: exactly the same price breakout cannot trade under a weak seasonal score.");
        AreEqual(0, KeltnerSeasonalStrategy.GetEntrySignal(0.5m, 105m, ema, atr, multiplier, threshold, 0m),
            "Seasonal equality with threshold must not pass the strict gate.");
        AreEqual(0, KeltnerSeasonalStrategy.GetEntrySignal(0.6m, 104m, ema, atr, multiplier, threshold, 0m),
            "Exactly the Keltner upper band is not a breakout.");
        AreEqual(-1, KeltnerSeasonalStrategy.GetEntrySignal(-0.8m, 95m, ema, atr, multiplier, threshold, 0m),
            "September: Close < EMA - 2 ATR and seasonal -0.8 must permit the short.");
        AreEqual(0, KeltnerSeasonalStrategy.GetEntrySignal(-0.4m, 95m, ema, atr, multiplier, threshold, 0m),
            "August: identical downside breakout cannot enter short.");
        AreEqual(0, KeltnerSeasonalStrategy.GetEntrySignal(-0.8m, 96m, ema, atr, multiplier, threshold, 0m),
            "Exactly the lower Keltner band is not a breakout.");
        AreEqual(0, KeltnerSeasonalStrategy.GetEntrySignal(0.6m, 105m, ema, atr, multiplier, threshold, 1m),
            "A held long must not duplicate a long entry.");
        AreEqual(1, KeltnerSeasonalStrategy.GetEntrySignal(0.6m, 105m, ema, atr, multiplier, threshold, -1m),
            "A long signal may reverse an existing short.");
        AreEqual(-1, KeltnerSeasonalStrategy.GetEntrySignal(-0.8m, 95m, ema, atr, multiplier, threshold, 1m),
            "A short signal may reverse an existing long.");
    }

    [TestMethod]
    [TestCategory("Shard01")]
    public async Task Point1Part01_0305_DynamicRsiUsesActualPercentStop()
    {
        await Replay("0305_RSI_Dynamic_Overbought_Oversold", (strategy, _) =>
        {
            AreEqual(14, Convert.ToInt32(strategy.Parameters["RsiPeriod"].Value));
            AreEqual(50, Convert.ToInt32(strategy.Parameters["MovingAvgPeriod"].Value));
            AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StdDevMultiplier"].Value));
            AreEqual(48, Convert.ToInt32(strategy.Parameters["CooldownBars"].Value));
            AreEqual(TimeSpan.FromMinutes(5).TimeFrame(), strategy.Parameters["CandleType"].Value);
        }, replayDuration: TimeSpan.FromDays(31));
        await CheckPercentStopBetweenBars("0305_RSI_Dynamic_Overbought_Oversold",
            duration: TimeSpan.FromDays(31));
    }

    [TestMethod]
    [TestCategory("Shard03")]
    public async Task Point1Part01_0307_AdaptiveMacdDefaultAndNativeStop()
    {
        await Replay("0307_MACD_Adaptive_Histogram", (strategy, _) =>
        {
            AreEqual(12, Convert.ToInt32(strategy.Parameters["FastPeriod"].Value));
            AreEqual(26, Convert.ToInt32(strategy.Parameters["SlowPeriod"].Value));
            AreEqual(9, Convert.ToInt32(strategy.Parameters["SignalPeriod"].Value));
            AreEqual(20, Convert.ToInt32(strategy.Parameters["HistogramAvgPeriod"].Value));
            AreEqual(2m, Convert.ToDecimal(strategy.Parameters["StdDevMultiplier"].Value));
            AreEqual(16, Convert.ToInt32(strategy.Parameters["CooldownBars"].Value));
            AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
        }, replayDuration: TimeSpan.FromDays(31));

        // The stop has to close a real execution between *15-minute* signal bars.
        await CheckPercentStopBetweenBars("0307_MACD_Adaptive_Histogram",
            duration: TimeSpan.FromDays(31),
            expectedFrame: TimeSpan.FromMinutes(15));
    }

}
