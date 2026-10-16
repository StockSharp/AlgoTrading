namespace StockSharp.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Messages;

public abstract partial class StrategyTests
{

    private const string Point1Part10GraalKey = "3846_Graal_Fractal_Channel";

    [TestMethod]
    [TestCategory("Shard06")]
    public async Task S3846_GraalEntriesRequireConfirmedFiveBarFractals()
    {
        var bars = new Queue<(decimal high, decimal low)>();
        var completed = 0;
        var entries = 0;
        var fills = 0;
        var invalid = new List<string>();
        Sides? latestFractal = null;
        var signalBar = -100;
        var ordersThisBar = 0;

        await Replay(Point1Part10GraalKey, (strategy, _) =>
        {
            var expected = new (string name, object value)[]
            {
                ("OrderVolume", 0.1m), ("StopLossPips", 500m),
                ("TakeProfitPips", 500m), ("OffsetPips", 5m),
                ("ChannelPeriod", 14), ("UseFractalChannel", false),
                ("DepthPercent", 25m), ("UseHighLowChannel", false),
                ("OrientationPercent", 20m), ("AllowFlatTrading", true),
                ("FlatThresholdPips", 20m), ("UseWilliamsExit", false),
                ("WilliamsPeriod", 14), ("WilliamsThreshold", 30m),
                ("UseCounterOrders", false), ("SinglePosition", false),
                ("SignalAgeLimit", 3),
            };
            foreach (var (name, value) in expected)
            {
                IsTrue(strategy.Parameters.TryGetValue(name, out var param), $"Missing {name}.");
                if (value is bool)
                    AreEqual(value, param.Value, name);
                else
                    AreEqual(Convert.ToDecimal(value), Convert.ToDecimal(param.Value), name);
            }
            AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);

            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State != CandleStates.Finished)
                    return;
                completed++;
                ordersThisBar = 0;
                bars.Enqueue((candle.HighPrice, candle.LowPrice));
                if (bars.Count > 5)
                    bars.Dequeue();
                if (bars.Count != 5)
                    return;
                var window = bars.ToArray();
                var high = window.Where((_, i) => i != 2).All(b => window[2].high > b.high);
                var low = window.Where((_, i) => i != 2).All(b => window[2].low < b.low);
                if (high != low)
                {
                    latestFractal = high ? Sides.Sell : Sides.Buy;
                    signalBar = completed;
                }
                else if (high)
                {
                    latestFractal = null;
                }
            };
            strategy.OrderRegistering += order =>
            {
                if (order.Comment != "Graal fractal entry")
                    return; // Native protection is checked by the existing replay harness.
                entries++;
                ordersThisBar++;
                if (order.Type != OrderTypes.Market || completed < 14 ||
                    latestFractal is null || order.Side != latestFractal ||
                    completed - signalBar > 3)
                    invalid.Add($"Unconfirmed Graal entry at completed bar {completed}: {order.Side} {order.Volume}.");
                if (ordersThisBar != 1)
                    invalid.Add($"Multiple entries at bar {completed}.");
                var baseVolume = 0.1m;
                if (strategy.Security.VolumeStep is decimal step && step > 0m)
                    baseVolume = decimal.Floor(baseVolume / step) * step;
                if (strategy.Security.MinVolume is decimal min && min > 0m)
                    baseVolume = Math.Max(baseVolume, min);
                var expectedVolume = baseVolume + (order.Side == Sides.Buy && strategy.Position < 0m ||
                    order.Side == Sides.Sell && strategy.Position > 0m ? Math.Abs(strategy.Position) : 0m);
                if (order.Volume != expectedVolume)
                    invalid.Add($"Wrong net order volume: {order.Volume}; expected {expectedVolume}.");
                latestFractal = null; // Consume the signal after one submitted entry.
            };
            strategy.OwnTradeReceived += (_, trade) =>
            {
                if (trade.Trade is not null && trade.Order?.Comment == "Graal fractal entry")
                    fills++;
            };
        }, TimeSpan.FromDays(31));

        AreEqual(0, invalid.Count, string.Join(Environment.NewLine, invalid.Take(12)));
        IsTrue(entries > 0 && fills > 0, "Require fractal entry orders and real fills on the packaged history.");
    }


    [TestMethod]
    [TestCategory("Shard06")]
    public async Task S3854_Ema612OnlyUsesCompletedCrossingsAndClosesWithoutReversing()
    {
        const string key = "3854_Ema_6_12";
        var closes = new List<decimal>();
        decimal? fast = null;
        decimal? slow = null;
        decimal? priorFast = null;
        decimal? priorSlow = null;
        var expected = (Sides?)null;
        var expectedVolume = 0m;
        var orders = 0;
        var entries = 0;
        var exits = 0;
        var fills = 0;
        var violations = new List<string>();
        await Replay(key, (strategy, _) =>
        {
            foreach (var (name, value) in new (string, object)[]
            {
                ("ShortEmaLength", 6), ("LongEmaLength", 12),
                ("OrderVolume", 1m), ("UseCloseSignals", true),
                ("TrailingStopSteps", 0m), ("TakeProfitSteps", 0m),
            })
            {
                IsTrue(strategy.Parameters.TryGetValue(name, out var parameter), $"Missing {name}.");
                if (value is bool) AreEqual(value, parameter.Value, name);
                else AreEqual(Convert.ToDecimal(value), Convert.ToDecimal(parameter.Value), name);
            }
            AreEqual(TimeSpan.FromHours(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State != CandleStates.Finished)
                    return;
                expected = null;
                closes.Add(candle.ClosePrice);
                decimal? Ema(int period, decimal? previous)
                    => closes.Count < period ? null
                        : closes.Count == period ? closes.TakeLast(period).Average()
                        : previous + (candle.ClosePrice - previous.Value) * 2m / (period + 1);
                fast = Ema(6, fast);
                slow = Ema(12, slow);
                if (fast is not decimal f || slow is not decimal s ||
                    priorFast is not decimal pf || priorSlow is not decimal ps)
                {
                    priorFast = fast;
                    priorSlow = slow;
                    return;
                }
                var bullish = pf <= ps && f > s;
                var bearish = pf >= ps && f < s;
                if (bullish && strategy.Position == 0m)
                {
                    expected = Sides.Buy;
                    expectedVolume = 1m;
                    entries++;
                }
                else if (bearish && strategy.Position == 0m)
                {
                    expected = Sides.Sell;
                    expectedVolume = 1m;
                    entries++;
                }
                else if (bullish && strategy.Position < 0m)
                {
                    expected = Sides.Buy;
                    expectedVolume = Math.Abs(strategy.Position);
                    exits++;
                }
                else if (bearish && strategy.Position > 0m)
                {
                    expected = Sides.Sell;
                    expectedVolume = strategy.Position;
                    exits++;
                }
                priorFast = f;
                priorSlow = s;
            };
            strategy.OrderRegistering += order =>
            {
                // Default zero protection means each submission must come from a cross.
                orders++;
                if (expected != order.Side || expectedVolume != order.Volume ||
                    order.Type != OrderTypes.Market)
                    violations.Add($"Unexpected order on {strategy.CurrentTime:O}: {order.Side} {order.Volume}; expected {expected} {expectedVolume}.");
                expected = null;
            };
            strategy.OwnTradeReceived += (_, trade) =>
            {
                if (trade.Trade is not null)
                    fills++;
            };
        }, TimeSpan.FromDays(31));
        AreEqual(0, violations.Count, string.Join(Environment.NewLine, violations.Take(12)));
        IsTrue(entries > 0 && exits > 0 && fills > 0,
            $"Exercise default long/short crossover entries, real fills and opposite-cross exits: {entries}/{exits}/{fills}.");
        TestContext.WriteLine($"{key}: {orders} orders, {fills} fills, {entries} entry signals and {exits} closing signals.");
    }


    [TestMethod]
    [TestCategory("Shard07")]
    public async Task S3855_FridayRequiresPriorDailyDirectionAndOneEntryAtHourSeven()
    {
        const string key = "3855_E_Friday_Session";
        var entries = 0;
        var fills = 0;
        var seenDates = new HashSet<DateTime>();
        (DateTime date, decimal open, decimal close)? previousDaily = null;
        var violations = new List<string>();

        await Replay(key, (strategy, _) =>
        {
            foreach (var (name, value) in new (string, object)[]
            {
                ("HourOpen", 7), ("HourClose", 19), ("StopLossPoints", 75m),
                ("TakeProfitPoints", 0m), ("TrailingStopPoints", 60m),
                ("TrailingStepPoints", 5m), ("UseClosePositions", true),
                ("UseTrailing", true), ("ProfitTrailing", true),
            })
            {
                IsTrue(strategy.Parameters.TryGetValue(name, out var p), $"Missing {name}.");
                if (value is bool)
                    AreEqual(value, p.Value, name);
                else
                    AreEqual(Convert.ToDecimal(value), Convert.ToDecimal(p.Value), name);
            }
            AreEqual(TimeSpan.FromDays(1).TimeFrame(), strategy.Parameters["DailyCandleType"].Value);
            AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), strategy.Parameters["IntradayCandleType"].Value);

            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State != CandleStates.Finished)
                    return;
                if (candle.CloseTime - candle.OpenTime >= TimeSpan.FromHours(23))
                    previousDaily = (candle.OpenTime.Date, candle.OpenPrice, candle.ClosePrice);
            };
            strategy.OrderRegistering += order =>
            {
                if (order.Comment != "EFriday entry")
                    return;
                entries++;
                var now = strategy.CurrentTime;
                if (now.DayOfWeek != DayOfWeek.Friday || now.Hour != 7)
                    violations.Add($"Entry outside Friday at 07:00 hour: {now:O}.");
                if (!seenDates.Add(now.Date))
                    violations.Add($"Duplicate entry on Friday {now:yyyy-MM-dd}.");
                if (previousDaily is not { } day || day.date != now.Date.AddDays(-1))
                    violations.Add($"Entry missing the previous completed Thursday daily candle on {now:yyyy-MM-dd}.");
                else if (day.close == day.open ||
                         order.Side != (day.close < day.open ? Sides.Buy : Sides.Sell))
                    violations.Add($"Wrong contrarian side {order.Side} for Thursday {day.open}/{day.close}.");
                if (order.Type != OrderTypes.Market)
                    violations.Add("Friday entry must be market.");
            };
            strategy.OwnTradeReceived += (_, trade) =>
            {
                if (trade.Order?.Comment == "EFriday entry" && trade.Trade is not null)
                    fills++;
            };
        }, TimeSpan.FromDays(31));

        AreEqual(0, violations.Count, string.Join(Environment.NewLine, violations.Take(12)));
        IsTrue(entries > 0 && fills > 0, "The archive must exercise Friday entries and actual fills.");
        TestContext.WriteLine($"{key}: {entries} Friday entries, {fills} actual fills.");
    }


    [TestMethod]
    [TestCategory("Shard02")]
    public async Task S3858_TrendScalperNeedsPriorBandSweepAndMovingAverageAgreement()
    {
        const string key = "3858_Trend_Scalper";
        var candles = new Queue<(decimal close, decimal high, decimal low)>();
        decimal? ema = null;
        Sides? signal = null;
        var entries = 0;
        var fills = 0;
        var violations = new List<string>();
        await Replay(key, (strategy, _) =>
        {
            foreach (var (name, value) in new (string, decimal)[]
            {
                ("FastLength", 6m), ("SlowLength", 12m),
                ("BreakoutWindow", 6m), ("FixedVolume", 0.1m),
                ("MoneyManagementMode", 0m), ("MoneyManagementRisk", 40m),
                ("StopLossPoints", 50m),
            })
            {
                IsTrue(strategy.Parameters.TryGetValue(name, out var p), $"Missing {name}.");
                AreEqual(value, Convert.ToDecimal(p.Value), name);
            }
            AreEqual(TimeSpan.FromMinutes(15).TimeFrame(), strategy.Parameters["CandleType"].Value);
            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State != CandleStates.Finished)
                    return;
                signal = null;
                var prior = candles.ToArray();
                candles.Enqueue((candle.ClosePrice, candle.HighPrice, candle.LowPrice));
                if (candles.Count > 12)
                    candles.Dequeue();
                if (candles.Count < 6)
                    return;
                var newest = candles.ToArray();
                if (newest.Length == 6)
                    ema = newest.Average(x => x.close);
                else
                    ema += (candle.ClosePrice - ema.Value) * 2m / 7m;
                if (prior.Length < 12)
                    return;
                var slow = prior.TakeLast(11).Sum(x => x.close) / 12m + candle.ClosePrice / 12m;
                var band = prior.TakeLast(6).ToArray();
                if (strategy.Position == 0m && ema > slow && candle.LowPrice <= band.Min(x => x.low))
                    signal = Sides.Buy;
                else if (strategy.Position == 0m && ema < slow && candle.HighPrice >= band.Max(x => x.high))
                    signal = Sides.Sell;
            };
            strategy.OrderRegistering += order =>
            {
                if (order.Comment != "Trend scalper entry")
                    return;
                entries++;
                if (order.Side != signal || order.Type != OrderTypes.Market)
                    violations.Add($"Entry without preceding breakout and trend filter at {strategy.CurrentTime:O}.");
                var volume = 0.1m;
                if (strategy.Security.VolumeStep is decimal step && step > 0m)
                    volume = decimal.Floor(volume / step) * step;
                if (strategy.Security.MinVolume is decimal min && min > 0m)
                    volume = Math.Max(volume, min);
                if (order.Volume != volume)
                    violations.Add($"Wrong fixed-volume market entry: {order.Volume}; expected {volume}.");
                signal = null;
            };
            strategy.OwnTradeReceived += (_, trade) =>
            {
                if (trade.Trade is not null && trade.Order?.Comment == "Trend scalper entry")
                    fills++;
            };
        }, TimeSpan.FromDays(31));

        AreEqual(0, violations.Count, string.Join(Environment.NewLine, violations.Take(12)));
        IsTrue(entries > 0 && fills > 0, "Require executable trend-and-band sweeps with real fills.");
    }


    [TestMethod]
    [TestCategory("Shard04")]
    public async Task S3852_FarhadCrabEntryNeedsNineBarTypicalEmaOrOpenSma()
    {
        const string key = "3852_Farhad_Crab";
        var typical = new List<decimal>();
        var opens = new Queue<decimal>();
        decimal? ema = null;
        var entrySignals = 0;
        var entryFills = 0;
        var invalid = new List<string>();
        Sides? entrySide = null;
        await Replay(key, (strategy, _) =>
        {
            foreach (var (name, defaultValue) in new (string name, decimal expected)[]
            {
                ("DailyMaPeriod", 55m), ("OrderVolume", 0.1m),
                ("LongTakeProfitPips", 10m), ("ShortTakeProfitPips", 10m),
                ("LongTrailingStopPips", 8m), ("ShortTrailingStopPips", 8m),
            })
            {
                IsTrue(strategy.Parameters.TryGetValue(name, out var p), $"Missing {name}.");
                AreEqual(defaultValue, Convert.ToDecimal(p.Value), name);
            }
            AreEqual(TimeSpan.FromMinutes(1).TimeFrame(), strategy.Parameters["CandleType"].Value);
            // The packaged replay may not span 55 completed daily candles.
            // Verify the published default above; test mechanics with a reachable period.
            SetParam(strategy, "DailyMaPeriod", 5);
            strategy.CandleReceived += (_, candle) =>
            {
                if (candle.State != CandleStates.Finished ||
                    candle.CloseTime - candle.OpenTime > TimeSpan.FromHours(1))
                    return;
                entrySide = null;
                var midpoint = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
                typical.Add(midpoint);
                opens.Enqueue(candle.OpenPrice);
                if (opens.Count > 9)
                    opens.Dequeue();
                if (typical.Count == 9)
                    ema = typical.Average();
                else if (typical.Count > 9)
                    ema += (midpoint - ema.Value) * 2m / 10m;
                if (opens.Count < 9 || ema is null || strategy.Position != 0m)
                    return;
                if (candle.LowPrice > ema.Value)
                    entrySide = Sides.Buy;
                else if (candle.HighPrice < opens.Average())
                    entrySide = Sides.Sell;
            };
            strategy.OrderRegistering += order =>
            {
                if (order.Comment != "Farhad Crab entry")
                    return;
                entrySignals++;
                if (entrySide is null || order.Side != entrySide ||
                    order.Type != OrderTypes.Market)
                    invalid.Add($"Unconfirmed Farhad entry: {order.Side} at {strategy.CurrentTime:O}.");
                entrySide = null;
            };
            strategy.OwnTradeReceived += (_, trade) =>
            {
                if (trade.Order?.Comment == "Farhad Crab entry" && trade.Trade is not null)
                    entryFills++;
            };
        }, TimeSpan.FromDays(31));
        AreEqual(0, invalid.Count, string.Join(Environment.NewLine, invalid.Take(12)));
        IsTrue(entrySignals > 0 && entryFills > 0,
            "Farhad Crab must execute real orders on the reachable mechanics profile.");
    }

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
