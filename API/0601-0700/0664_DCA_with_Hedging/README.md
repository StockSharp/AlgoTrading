# DCA Strategy with Hedging
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy enters long after three consecutive candles close above the EMA and enters short after three consecutive candles close below it. Additional positions are added when price moves against the latest entry by a given percentage. The whole position is closed once price moves by the take profit percentage from the average entry price.

## Parameters
- Candle Type
- EMA length
- DCA interval %
- Take profit %
- Initial position size

