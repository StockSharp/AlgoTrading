# MultiLayer Awesome Oscillator Saucer
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implements a bullish multi-layer strategy based on the Awesome Oscillator saucer pattern and fractal trend detection. The strategy counts consecutive saucer signals and places up to five layered buy stop orders above price. Positions are closed when the trend reverses.

## Parameters
- **EMA Length** – period of the EMA filter.
- **Candle Type** – type of candles.
- **Trade Start** – start of trading period.
- **Trade Stop** – end of trading period.
