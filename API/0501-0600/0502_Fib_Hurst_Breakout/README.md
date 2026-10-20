# Fib Hurst Breakout

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Execution uses intraday CandleType. Fibonacci range is the highest high and lowest low of the previous LookbackPeriod complete UTC daily candles, excluding the signal day. Levels are L+0.618×(H−L) and L+0.382×(H−L). Daily Hurst uses the previous HurstPeriod daily closes: H = log(R/S)/log(N), R is the range of cumulative deviations from the window mean, S is population standard deviation. A flat/zero-variance window blocks entry. Compare the previous intraday close and current close against the same current daily level: cross above 61.8% and H>0.5 buys, cross below 38.2% and H<0.5 shorts. Enter only from flat, with Volume; MaxTradesPerDay counts entry order attempts by historical UTC date, MaxTotalTrades counts the run. Freeze native quote-based SL = RiskPercent% and TP = RiskPercent×RiskReward% from actual execution. No opposite-signal reversal or cooldown.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(15).TimeFrame() |
| `HurstPeriod` | 50 |
| `LookbackPeriod` | 20 |
| `MaxTradesPerDay` | 5 |
| `MaxTotalTrades` | 510 |
| `RiskPercent` | 2 |
| `RiskReward` | 2 |
| `DailyHistoryFile` | not supplied |

## Input data

Optional `DailyHistoryFile` CSV: `Date,High,Low,Close`, date `yyyy-MM-dd`. Dates must be unique, 0 < Low ≤ Close ≤ High. Only dates before the intraday signal day are eligible. Live completed daily bars replace their own date; future rows cannot influence an earlier decision.

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
