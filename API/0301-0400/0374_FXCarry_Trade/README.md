# FX Carry Trade

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Monthly carry = observed annual BaseRate − QuoteRate. Long the TopK highest differentials and short the TopK lowest; require at least 2×TopK eligible pairs to keep the baskets disjoint. Each side receives half the gross USD budget, equally divided. Use the explicitly observed UsdPerUnit to convert USD exposure into native pair units, including pairs whose quote currency is not USD. Flatten names that leave the baskets and reconcile continuing positions at the next review.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

The publication cutoff is the latest actual CloseTime among synchronized finished candles. A trading session can finish before the configured candle interval has elapsed.

## Input data

RatesFile is required CSV: `SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit`. PublishedAt is UTC `yyyy-MM-ddTHH:mm:ssZ`; rates use the same annual decimal units. UsdPerUnit is the positive USD value of one native traded unit at that snapshot. Only the latest published snapshot no older than MaxRateAgeDays is eligible. Duplicate security/publication pairs are rejected. Candle prices cannot substitute for interest rates.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `TopK` | `3` |
| `RatesFile` | required |
| `MaxRateAgeDays` | `45` |

## Execution

Capital is a fixed USD budget and Leverage controls gross exposure. Use RatesFile.UsdPerUnit for sizing and the MinTradeUsd filter. Round toward zero to VolumeStep, reconcile against actual fills and wait for pending orders. Respect MinVolume/MaxVolume and split large orders across later candles. Complete liquidation bypasses MinTradeUsd. No stop loss or take profit.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
