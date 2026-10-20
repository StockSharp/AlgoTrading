# Bearish Wick Reversal

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

This is a long-only lower-wick reversal on finished one-minute candles. While flat, buy `Volume` when `Close < Open` and `100 * (Low - Close) / Close <= Threshold` (default `-1`, meaning a lower wick of at least one percent of close).

Entries require the candle's UTC opening time to be inside the inclusive `StartTime`–`EndTime` window. When `UseEmaFilter` is enabled, also require a formed `EMA(EmaPeriod)` and `Close > EMA`; when disabled, EMA warm-up does not postpone entries.

Close the entire filled long when a completed candle closes above the previous completed candle's high. The entry date window and EMA filter do not block exits. There are no short entries, hidden bar cooldown, automatic take-profit or stop-loss. An active order prevents duplicate submissions.

## Parameters

| Parameter | Default |
|---|---|
| `Threshold` | `-1` |
| `UseEmaFilter` | `false` |
| `EmaPeriod` | `200` |
| `CandleType` | `1m` |
| `StartTime` | `2014-01-01T00:00:00Z` |
| `EndTime` | `2099-01-01T00:00:00Z` |
