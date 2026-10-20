# Momentum Factor Stocks

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

At each monthly review require LookbackDays + 1 completed daily closes. Momentum = P[d−SkipDays] / P[d−LookbackDays] − 1; SkipDays must be less than LookbackDays. This excludes the latest SkipDays daily return intervals without subtracting two separate ROC indicators. Long the highest Quintile bucket and short the lowest, floor(eligible count / Quintile) securities on each side. Hold rounded native quantities until the next monthly review.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

## Input data

PriceHistoryFile is an optional CSV of positive daily closes: SecurityId,Date,Close. Dates use yyyy-MM-dd. Current and future dates cannot prefill a decision; live finished closes replace each UTC date. Duplicate dates are rejected. It is needed to warm long lookbacks when the execution archive is short.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | optional — not supplied |
| `LookbackDays` | `252` |
| `SkipDays` | `21` |
| `Quintile` | `5` |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

Long and short baskets each receive half of Capital × Leverage. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
