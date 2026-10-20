# Ema50 Crossover Monthly Dca

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

Use finished weekly candles and a formed EmaLength EMA of closes, seeded with its first complete-window arithmetic mean. Eligible closes begin at StartDate. Credit DcaAmount once in the first observed eligible calendar month and for elapsed months thereafter, including during EMA warmup. Do not fabricate decades of contributions before the first observed candle. Above EMA invest accumulated cash in native rounded units; below EMA sell the whole actual long; equality holds. Cash tracks gross actual fills and sale proceeds; it is a strategy allocation ledger, not an account deposit or financing model. Await pending orders, respect volume limits, remain long-only and retain unspent cash. No RSI filter, cooldown or protective stops.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(10080).TimeFrame() |
| `EmaLength` | 50 |
| `DcaAmount` | 100000 |
| `StartDate` | 1980-01-01 |

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
