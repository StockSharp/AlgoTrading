# Professional ORB Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implements an Opening Range Breakout strategy. The high and low between 09:15 and a configurable duration form the range. After the range is completed and wide enough, breakouts above or below trigger long or short entries. Positions use an ATR-based stop-loss, a fixed profit target in points, and are closed at session end. The number of trades per day is limited.
