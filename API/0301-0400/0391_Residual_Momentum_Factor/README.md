# Residual Momentum Factor

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

ResidualMomentumFile supplies residual momentum computed by the external research/data source. Do not estimate residuals with an OHLC or benchmark-return proxy. Each month rank the latest eligible published scores descending, buy floor(n/Decile) leaders and short the same number of laggards. Require at least Decile eligible securities.

Set Universe explicitly. Prices and Capital use USD, with one price unit per traded unit. Capital is a fixed budget; Leverage controls gross exposure. No stop loss or take profit is added.

Evaluate only synchronized finished candles. Statistics count distinct UTC dates, replacing earlier intraday closes of the same date. Rebalance once on the first eligible synchronized candle of each month; an unformed input delays the decision. Equal scores are ordered by Security.Id.

Long and short baskets each receive half of Capital × Leverage. Divide each side equally, convert USD to units at the signal close and round toward zero to VolumeStep. Reconcile against actual filled positions, wait for pending orders, respect MinVolume/MaxVolume and split large adjustments across later candles. MinTradeUsd filters small adjustments; full liquidation bypasses it.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | 100000 |
| `Leverage` | 1 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `ResidualMomentumFile` | required |
| `Decile` | 10 |
| `MaxScoreAgeDays` | 400 |

## Input data

The required score CSV has header `SecurityId,PeriodEnd,PublishedAt,Score`. PeriodEnd uses `yyyy-MM-dd`; PublishedAt is UTC `yyyy-MM-ddTHH:mm:ssZ`; Score is a decimal. Reject duplicate revisions or publication before period end. Select the newest fiscal/measurement period, then latest revision published by the actual synchronized candle CloseTime. `MaxScoreAgeDays` counts from PeriodEnd. Missing, stale or undersized inputs delay the review and retain existing holdings; prices do not substitute for scores.

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
