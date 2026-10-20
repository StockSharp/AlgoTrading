# Commodity Momentum

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Let M be the decision month and P[K] the last observed daily close in a completed calendar month K. Momentum = P[M − SkipMonths − 1] / P[M − LookbackMonths − 1] − 1. All months between the endpoints must be present. Defaults compare January of the current year with February of the previous year at a March rebalance, excluding February and unfinished March. Buy up to TopN highest-ranked instruments, long only, using equal shares of Capital × Leverage. Negative momentum remains eligible; there is no absolute-return filter. Close removed names at the next monthly rebalance.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

## Input data

PriceHistoryFile is an optional CSV of positive daily closes: SecurityId,Date,Close. Dates use yyyy-MM-dd. Current and future dates cannot prefill a decision; live finished closes replace each UTC date. Duplicate dates are rejected. It is needed to warm long lookbacks when the execution archive is short.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `PriceHistoryFile` | optional — not supplied |
| `TopN` | `5` |
| `LookbackMonths` | `12` |
| `SkipMonths` | `1` |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

See the strategy-specific allocation above. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Verification

Tests use only packaged BTC/TON execution history. Explicit financial reports, option observations and price warmup fixtures test mechanics; they are not actual company fundamentals or option-market history. Reduced lookbacks, two buckets and March annual scheduling are identified test profiles, not proof of trading at unchanged long-horizon defaults. No return or profitability claim is made.
