# MACD Aggressive Scalp Simple Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implements a scalping strategy using the MACD histogram with a 50‑period EMA filter.

- Goes long when the MACD histogram crosses above zero and price is above the EMA.
- Goes short when the histogram crosses below zero and price is below the EMA.
- Closes positions when histogram momentum reverses.
