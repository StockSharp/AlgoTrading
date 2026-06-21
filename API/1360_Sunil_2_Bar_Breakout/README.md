# Sunil 2 Bar Breakout Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy places stop orders based on a two-bar breakout pattern. A long stop is placed when the latest close is above the previous close and the prior high exceeds the highs of the two bars before it. A short stop is placed when the latest close is below the previous close and the prior low is lower than the lows of the two bars before it. Exits are triggered by stop-loss levels at the opposite end of the signal bar.

