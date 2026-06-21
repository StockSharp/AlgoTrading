# Stoch TP TS V3103
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trades stochastic oscillator crossovers at a specific minute each hour. When %K crosses %D within the 20-80 range, the strategy opens a position. It applies an initial stop loss and a take profit at three times the activation distance, then switches to a trailing stop once price moves by the configured offset.
