# Trade Entry Detector, Wick to Body Ratio Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy uses Bollinger Bands and the wick-to-body ratio of daily candles to detect entries. A long position is opened when a candle's lower wick is large enough and pierces the lower band. A short position is opened on the opposite condition. Positions are closed at the opposite band or when the swing level is broken.
