# Hull MA Implied Volatility Breakout

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

HMA = WMA(2 × WMA(Close, floor(HmaPeriod/2)) − WMA(Close, HmaPeriod), floor(sqrt(HmaPeriod))). Require a formed HMA and a preceding formed HMA value. From flat, buy on rising HMA or short on falling HMA only when observed IV > prior mean + IVMultiplier × σ. IV is supplied option-market implied volatility in one consistent unit, not realized candle range. A falling HMA exits a long; a rising HMA exits a short even if IV is missing or no longer extreme. Equal HMA values give no signal.

Compute the mean and sample standard deviation over the previous N consecutive observations, excluding the current one: σ = sqrt(Σ(x − mean)² / (N − 1)). Start only after the complete window; no random or price-based substitute is generated.

## Input data

Provide the required CSV with header SecurityId,Time,Value. Time is the signal candle UTC open timestamp in yyyy-MM-ddTHH:mm:ssZ format. Values are observed statistics for that execution instrument, finite, nonnegative and unique per timestamp. The strategy consumes only the matching current observation. A missing observation clears the filter window and blocks entries until it is warm again; signal exits remain active.

## Parameters

| Parameter | Default |
|---|---|
| `HmaPeriod` | `9` |
| `IVPeriod` | `20` |
| `IVMultiplier` | `2` |
| `IVHistoryFile` | required |
| `AtrPeriod` | `14` |
| `StopLossAtr` | `2` |
| `CandleType` | `TimeSpan.FromMinutes(15).TimeFrame()` |

## Execution

AtrPeriod = 14 uses Wilder ATR. At each flat-position entry freeze distance = ATR(signal) × StopLossAtr. Quote-driven native local market protection anchors this distance to the actual fill and can exit between signal candles. Later ATR changes do not move the stop. Zero disables protection for a fresh run. Exit the whole actual remaining position on a signal reversal; do not reverse or pyramid in that same decision.

## Verification

Tests use only packaged BTC/TON execution history. Explicit financial reports, option observations and price warmup fixtures test mechanics; they are not actual company fundamentals or option-market history. Reduced lookbacks, two buckets and March annual scheduling are identified test profiles, not proof of trading at unchanged long-horizon defaults. No return or profitability claim is made.

