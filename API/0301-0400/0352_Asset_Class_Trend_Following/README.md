# Asset Class Trend Following

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

For every instrument, SMA is the arithmetic mean of the latest SmaLength distinct daily closes, including the current completed close. Hold long only when Close > SMA. At the monthly decision divide Capital × Leverage equally among eligible instruments; unused capital remains cash when none qualify. A later Close <= SMA liquidates that instrument completely without waiting for another month. It cannot re-enter until the next monthly allocation. No relative-strength benchmark is used.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

For intraday CandleType, daily statistics use only UTC dates strictly before the current signal date. The incomplete current day is excluded. A finished one-day candle may contribute its own completed date.

## Input data

PriceHistoryFile is an optional CSV of positive daily closes: SecurityId,Date,Close. Dates use yyyy-MM-dd. Current and future dates cannot prefill a decision; live finished closes replace each UTC date. Duplicate dates are rejected. It is needed to warm long lookbacks when the execution archive is short.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | optional — not supplied |
| `SmaLength` | `210` |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

See the strategy-specific allocation above. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Verification

Tests use only packaged BTC/TON execution history. Explicit financial reports, option observations and price warmup fixtures test mechanics; they are not actual company fundamentals or option-market history. Reduced lookbacks, two buckets and March annual scheduling are identified test profiles, not proof of trading at unchanged long-horizon defaults. No return or profitability claim is made.
