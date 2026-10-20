# Supertrend Put Call Ratio

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Use the native StockSharp SuperTrend with Period and Multiplier. From flat, buy when Close > SuperTrend and current PCR < prior mean − PCRMultiplier × σ; short when Close < SuperTrend and current PCR > prior mean + PCRMultiplier × σ. PCR is observed put volume / call volume. A long exits at Close < SuperTrend; a short at Close > SuperTrend, regardless of PCR availability or extremes. Equality gives no signal.

Compute the mean and sample standard deviation over the previous N consecutive observations, excluding the current one: σ = sqrt(Σ(x − mean)² / (N − 1)). Start only after the complete window; no random or price-based substitute is generated.

## Input data

Provide the required CSV with header SecurityId,Time,Value. Time is the signal candle UTC open timestamp in yyyy-MM-ddTHH:mm:ssZ format. Values are observed statistics for that execution instrument, finite, nonnegative and unique per timestamp. The strategy consumes only the matching current observation. A missing observation clears the filter window and blocks entries until it is warm again; signal exits remain active.

## Parameters

| Parameter | Default |
|---|---|
| `Period` | `10` |
| `Multiplier` | `3` |
| `PCRPeriod` | `20` |
| `PCRMultiplier` | `2` |
| `PCRHistoryFile` | required |
| `AtrPeriod` | `14` |
| `StopLossAtr` | `2` |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` |

## Execution

AtrPeriod = 14 uses Wilder ATR. At each flat-position entry freeze distance = ATR(signal) × StopLossAtr. Quote-driven native local market protection anchors this distance to the actual fill and can exit between signal candles. Later ATR changes do not move the stop. Zero disables protection for a fresh run. Exit the whole actual remaining position on a signal reversal; do not reverse or pyramid in that same decision.

## Verification

Tests use only packaged BTC/TON execution history. Explicit financial reports, option observations and price warmup fixtures test mechanics; they are not actual company fundamentals or option-market history. Reduced lookbacks, two buckets and March annual scheduling are identified test profiles, not proof of trading at unchanged long-horizon defaults. No return or profitability claim is made.

