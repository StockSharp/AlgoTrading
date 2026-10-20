# Earnings Quality Factor

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Define earnings quality explicitly as (OperatingCashFlow − NetIncome) / TotalAssets. At the first eligible synchronized candle in RebalanceMonth, select the newest fiscal report and latest revision published by the signal candle close. Long the highest-quality Deciles bucket and short the lowest, floor(eligible count / Deciles) names per side. Rebalance once per year; keep actual holdings until the next review. Reports older than MaxReportAgeDays from PeriodEnd are ineligible.

Rebalance once per year, in RebalanceMonth, on the first eligible synchronized finished candle. Reports published later cannot affect an earlier decision. MaxReportAgeDays is measured from PeriodEnd.

All observations must be published by the signal candle close. Intraday execution uses daily prices strictly before its UTC date; a finished one-day candle contributes its own completed date. Never warm a decision with future CSV dates.

The publication cutoff is the latest actual CloseTime among synchronized finished candles. A trading session can finish before the configured candle interval has elapsed.

## Input data

FundamentalsFile is a required CSV: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd uses yyyy-MM-dd; PublishedAt uses yyyy-MM-ddTHH:mm:ssZ (UTC). Assets and shares must be positive. For each security select the newest fiscal period and latest revision published by the signal time. Reject duplicate revisions and publications before the fiscal period ends. No fundamental value is inferred from candles.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `RebalanceMonth` | `7` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | required |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

Long and short baskets each receive half of Capital × Leverage. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Verification

Tests run both languages using packaged BTC/TON execution data and explicit rate, financial, score, text and warmup fixtures. Calendar fixtures relocate archive OHLC/volume and provide deterministic execution quotes; they are not multi-year market history. Independent formulas, order direction/size, actual fills, residual positions and forbidden decision dates are checked. Two buckets, K=1 and AssetDecile=1 are declared mechanics profiles, not evidence of the complete production universe. No profitability claim is made.
