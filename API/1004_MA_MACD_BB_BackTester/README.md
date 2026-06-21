# MA MACD BB BackTester
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategy combining three selectable indicators: simple moving average crossover, MACD crossover, or Bollinger Bands breakout. Only one indicator mode is active at a time, and the trade direction can be long or short.

## Parameters
- `CandleType` — candle timeframe.
- `Indicator` — indicator to use (MA, MACD, BB).
- `Direction` — trade direction (Long or Short).
- `MaLength` — moving average period.
- `FastLength` — MACD fast EMA length.
- `SlowLength` — MACD slow EMA length.
- `SignalLength` — MACD signal length.
- `BbLength` — Bollinger Bands period.
- `BbMultiplier` — Bollinger Bands multiplier.
- `StartDate` — start date.
- `EndDate` — end date.
