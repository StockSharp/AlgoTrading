# RSI Buy Sell Force Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The strategy calculates RSI on the incoming candles and smooths it with an EMA.
It derives two lines, `cc` and `bb`, representing buying and selling pressure.
A long position is opened when `cc` crosses above `bb`, while a short position is opened when `cc` crosses below `bb`.
