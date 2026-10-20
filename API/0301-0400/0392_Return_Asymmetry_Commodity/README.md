# Return Asymmetry Commodity

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

For WindowDays completed daily returns r, U = Σmax(r,0), D = Σmax(−r,0). Rank by (U−D)/(U+D), using zero when U+D=0. This bounded score preserves the ordering of U/D, including all-up/all-down windows, without an arbitrary denominator floor. It is an up/down return balance, not statistical skewness. Buy TopN leaders and short TopN distinct laggards; require at least 2×TopN eligible securities.

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
| `PriceHistoryFile` | not supplied |
| `WindowDays` | 120 |
| `TopN` | 5 |

## Input data

Optional `PriceHistoryFile` CSV: `SecurityId,Date,Close`, positive close, distinct security/date, date `yyyy-MM-dd`. Intraday decisions use only dates before the current UTC date; a finished one-day candle contributes its own completed close. Future rows cannot prefill earlier decisions. Execution candles replace their own daily value.

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
