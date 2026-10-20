# Two X Spy Tips

[Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Trading rules

SpySecurity and TipsSecurity are distinct explicit signal streams in Universe; the execution Security also belongs to Universe. At the first synchronized finished candle of each month, both latest completed daily closes must be strictly above their own SmaLength-day SMA including that close. A formed failed filter consumes the monthly review. Missing warmup delays it. When flat buy Capital×Leverage/price rounded down to VolumeStep. Hold the long without exits, shorts or additional monthly accumulation. Capital and prices use USD; MinTradeUsd and native volume constraints apply.

## Parameters

| Parameter | Default |
|---|---|
| `Universe` | required |
| `Capital` | 100000 |
| `Leverage` | 2 |
| `MinTradeUsd` | 200 |
| `CandleType` | TimeSpan.FromMinutes(1440).TimeFrame() |
| `PriceHistoryFile` | not supplied |
| `SpySecurity` | required |
| `TipsSecurity` | required |
| `SmaLength` | 200 |

## Input data

Optional `PriceHistoryFile` CSV: `SecurityId,Date,Close`, positive close, distinct security/date, date `yyyy-MM-dd`. Intraday decisions use only dates before the current UTC date; a finished one-day candle contributes its own completed close. Future rows cannot prefill earlier decisions. Execution candles replace their own daily value.

## Verification

Both languages are checked with the packaged BTC/TON history, independent formulas, actual order sides/quantities, fills, positions and exits. Dated price/factor and calendar fixtures test mechanics; relocated OHLC/volume is not multi-year market history. Two rank buckets, TopN=1 and SMA=2 are explicit small-universe test profiles. Published long lookbacks/defaults are checked separately. No profitability claim is made.
