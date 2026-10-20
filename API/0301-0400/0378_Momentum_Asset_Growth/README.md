# Momentum Asset Growth

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

From the latest published annual report and the same fiscal date one year earlier calculate asset growth = TotalAssets[current] / TotalAssets[previous] − 1. Keep only the highest AssetDecile bucket, floor(eligible financial count / AssetDecile) securities. Within that subset compute MomLook-day momentum from MomLook + 1 completed daily closes ending before the first day of the decision month minus SkipMonths whole calendar months. Long the highest Quintile bucket and short the lowest; require at least Quintile fully warmed names after filtering. Rebalance monthly except January. January keeps existing holdings without allocation or forced liquidation.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

The publication cutoff is the latest actual CloseTime among synchronized finished candles. A trading session can finish before the configured candle interval has elapsed.

## Input data

FundamentalsFile is a required CSV: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd uses yyyy-MM-dd; PublishedAt uses yyyy-MM-ddTHH:mm:ssZ (UTC). Assets and shares must be positive. For each security select the newest fiscal period and latest revision published by the signal time. Reject duplicate revisions and publications before the fiscal period ends. No fundamental value is inferred from candles.

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
| `MomLook` | `252` |
| `SkipMonths` | `1` |
| `AssetDecile` | `10` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | required |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

Long and short baskets each receive half of Capital × Leverage. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
