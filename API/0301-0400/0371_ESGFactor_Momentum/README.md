# ESG Factor Momentum

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Universe must already contain ESG-screened securities; the strategy does not manufacture an ESG rating. Monthly momentum = P[d] / P[d−LookbackDays] − 1 from LookbackDays + 1 completed daily closes. Buy only the highest return, even if all returns are negative. At every monthly review completely liquidate all old positions and wait for their actual fills before buying the winner, including when the winner is unchanged. Use the full capital budget capped by reported portfolio value; no leverage or short positions.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

## Input data

PriceHistoryFile is an optional CSV of positive daily closes: SecurityId,Date,Close. Dates use yyyy-MM-dd. Current and future dates cannot prefill a decision; live finished closes replace each UTC date. Duplicate dates are rejected. It is needed to warm long lookbacks when the execution archive is short.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `PriceHistoryFile` | optional — not supplied |
| `LookbackDays` | `252` |

## Execution

Universe is explicit. Execution prices and Capital use USD per traded unit. Long-only exposure is capped at min(Capital, reported Portfolio.CurrentValue); an unavailable/nonpositive portfolio value leaves no buying budget. No leverage, stop loss or take profit.

Round target quantities toward zero to VolumeStep. Wait for actual liquidation fills before opening new positions; reconcile actual positions, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters nonzero target adjustments, while full liquidation bypasses it.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
