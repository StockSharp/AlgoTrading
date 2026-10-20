# Momentum Rev Vol

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

For each security use distinct completed daily closes P. M = P[t]/P[t−Lookback12]−1, R = P[t]/P[t−Lookback1]−1 and V is the sample standard deviation of the last VolWindow simple daily returns (denominator N−1). Score = WM×M − WR×R − WV×V, without cross-sectional normalization. Rank descending; buy floor(n/Decile) leaders and short the same number of laggards. Require at least Decile eligible securities. Freeze quantities between monthly reviews.

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
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | not supplied |
| `Lookback12` | 252 |
| `Lookback1` | 21 |
| `VolWindow` | 60 |
| `WM` | 1 |
| `WR` | 1 |
| `WV` | 1 |
| `Decile` | 10 |

## Input data

Optional `PriceHistoryFile` CSV: `SecurityId,Date,Close`, positive close, distinct security/date, date `yyyy-MM-dd`. Intraday decisions use only dates before the current UTC date; a finished one-day candle contributes its own completed close. Future rows cannot prefill earlier decisions. Execution candles replace their own daily value.

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
