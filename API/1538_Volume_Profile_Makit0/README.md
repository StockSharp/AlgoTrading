# Volume Profile Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This simplified volume profile strategy tracks session high, low and the point of control defined by the price of the candle with the highest volume. The strategy buys when price is above the point of control and sells when it is below. Positions are closed when price returns to the session mid level.

## Parameters
- **Candle Type** – timeframe of input candles.
