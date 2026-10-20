# F-Score Reversal

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

At each monthly review calculate Lookback-day return P[d] / P[d−Lookback] − 1. Among published FScore >= FHi buy the lowest return; if IsShortEnabled, among FScore <= FLo short the highest return. Thresholds are inclusive and FHi must exceed FLo. Scores are genuine published Piotroski inputs, integers 0..9, with the latest known period/revision and MaxReportAgeDays limit. Each selected leg receives half the gross budget; a missing or disabled side stays in cash. Close the whole old portfolio and wait for fills before opening the new monthly legs.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

The publication cutoff is the latest actual CloseTime among synchronized finished candles. A trading session can finish before the configured candle interval has elapsed.

## Input data

PriceHistoryFile is an optional CSV of positive daily closes: SecurityId,Date,Close. Dates use yyyy-MM-dd. Current and future dates cannot prefill a decision; live finished closes replace each UTC date. Duplicate dates are rejected. It is needed to warm long lookbacks when the execution archive is short.

FScoreFile is required CSV: `SecurityId,PeriodEnd,PublishedAt,FScore`. PeriodEnd is `yyyy-MM-dd`, PublishedAt is UTC `yyyy-MM-ddTHH:mm:ssZ`. Reject scores outside integer 0..9, duplicate revisions and publications before PeriodEnd. Choose the newest fiscal period and its latest published revision; MaxReportAgeDays counts from PeriodEnd.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | optional — not supplied |
| `Lookback` | `21` |
| `FHi` | `7` |
| `FLo` | `3` |
| `IsShortEnabled` | `true` |
| `MaxReportAgeDays` | `550` |
| `FScoreFile` | required |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

Round target quantities toward zero to VolumeStep. Wait for actual liquidation fills before opening new positions; reconcile actual positions, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters nonzero target adjustments, while full liquidation bypasses it.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
