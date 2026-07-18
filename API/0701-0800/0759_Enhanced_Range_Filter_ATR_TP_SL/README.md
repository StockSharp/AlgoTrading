# Enhanced Range Filter Strategy with ATR TP/SL
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy combines a custom range filter with ATR based take profit and stop loss levels.
Entries occur when price breaks the filter and all additional filters are satisfied:

- Volume above average
- RSI within configured boundaries
- Trend confirmation via EMA crossover
- Market not ranging based on ATR ratio

Positions are closed when the ATR based stop loss or take profit level is reached.
