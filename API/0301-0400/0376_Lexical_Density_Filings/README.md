# Lexical Density Filings

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Read the latest published plain-text filing for each security. Tokens are Unicode letter/mark sequences; normalize case with invariant .NET casing. Lexical density = distinct normalized tokens / all tokens, not candle compression or an inferred financial score. In February, May, August and November allocate only within the first three trading dates of the first security's exchange calendar. Delay within that window until at least Quintile filings are available; otherwise wait until the next review month. Long the highest-density bucket and short the lowest, floor(eligible count / Quintile) per side. Hold between reviews.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

The publication cutoff is the latest actual CloseTime among synchronized finished candles. A trading session can finish before the configured candle interval has elapsed.

## Input data

FilingsFile is required CSV: `SecurityId,PeriodEnd,PublishedAt,DocumentPath`. Dates use `yyyy-MM-dd` and UTC `yyyy-MM-ddTHH:mm:ssZ`. DocumentPath refers to a UTF-8 plain-text file, absolute or relative to the manifest directory. Paths cannot contain commas. Empty documents, duplicate revisions and publications before PeriodEnd are rejected. Choose the newest period/revision published by the signal close; limit age by MaxReportAgeDays. The first security must provide an exchange calendar, including holidays.

Word boundaries use Unicode text elements whose leading category is a letter or combining mark; punctuation and digits separate tokens. Case normalization is invariant; no stemming or accent normalization is applied.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FilingsFile` | required |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

Long and short baskets each receive half of Capital × Leverage. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
