# Accrual Anomaly

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Rank eligible annual reports by accruals = (NetIncome − OperatingCashFlow) / TotalAssets. Buy the lowest Deciles bucket and short the highest. Each extreme bucket contains floor(eligible count / Deciles) instruments; at least Deciles eligible reports are required. The default annual rebalance month is May.

Rebalance once per year, in RebalanceMonth, on the first eligible synchronized finished candle. Reports published later cannot affect an earlier decision. MaxReportAgeDays is measured from PeriodEnd.

## Input data

FundamentalsFile is a required CSV: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd uses yyyy-MM-dd; PublishedAt uses yyyy-MM-ddTHH:mm:ssZ (UTC). Assets and shares must be positive. For each security select the newest fiscal period and latest revision published by the signal time. Reject duplicate revisions and publications before the fiscal period ends. No fundamental value is inferred from candles.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `FundamentalsFile` | required |
| `RebalanceMonth` | `5` |
| `MaxReportAgeDays` | `550` |

## Execution

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

Long and short baskets each receive half of Capital × Leverage. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Verification

Tests use only packaged BTC/TON execution history. Explicit financial reports, option observations and price warmup fixtures test mechanics; they are not actual company fundamentals or option-market history. Reduced lookbacks, two buckets and March annual scheduling are identified test profiles, not proof of trading at unchanged long-horizon defaults. No return or profitability claim is made.
