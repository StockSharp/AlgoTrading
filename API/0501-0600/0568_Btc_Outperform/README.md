# BTC Outperform

[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Trading rules

Aggregates finished UTC daily candles into fixed periods. Weekly periods are anchored at Monday 1970-01-05; the 90-day periods are anchored at 1970-01-01. A period supplies a close only after every daily source candle from its start through its end is present. Partial startup periods and periods containing gaps do not publish a close.

Compare the most recent fully observed completed closes: weekly above quarterly buys; weekly below quarterly sells; equality holds the current position. From flat the order is `Volume`; a reversal is `Volume+abs(Position)`, leaving a complete opposite position. In-progress periods cannot alter the comparison. Pending orders block duplicate submissions. No automatic price stop or profit target is configured.

## Parameters

| Parameter | Default |
|---|---|
| `CandleType` | `1d` |
| `WeeklyDays` | `7` |
| `QuarterlyDays` | `90` |
