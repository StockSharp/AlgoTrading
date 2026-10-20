# Dollar Carry Trade

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Rank observed foreign-minus-USD annual interest differentials each month. For USD/base pairs (IsUsdBase=true), carry = QuoteRate − BaseRate; otherwise carry = BaseRate − QuoteRate. Select K lowest and K highest, requiring at least 2×K eligible pairs. Hold USD long against the lowest carry and USD short against the highest: the native order sign is positive for long USD in a USD/base pair and negative when USD is the quote. Each side receives half the gross USD budget. Pair orientation must remain constant across revisions.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

The publication cutoff is the latest actual CloseTime among synchronized finished candles. A trading session can finish before the configured candle interval has elapsed.

## Input data

RatesFile is required CSV: `SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit`. PublishedAt is UTC `yyyy-MM-ddTHH:mm:ssZ`; rates use the same annual decimal units. UsdPerUnit is the positive USD value of one native traded unit at that snapshot. Only the latest published snapshot no older than MaxRateAgeDays is eligible. Duplicate security/publication pairs are rejected. Candle prices cannot substitute for interest rates.

For this strategy append the required boolean column `IsUsdBase` to the rates header and every row (`true` or `false`).

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `K` | `3` |
| `RatesFile` | required |
| `MaxRateAgeDays` | `45` |

## Execution

Capital is a fixed USD budget and Leverage controls gross exposure. Use RatesFile.UsdPerUnit for sizing and the MinTradeUsd filter. Round toward zero to VolumeStep, reconcile against actual fills and wait for pending orders. Respect MinVolume/MaxVolume and split large orders across later candles. Complete liquidation bypasses MinTradeUsd. No stop loss or take profit.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
