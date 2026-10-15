# Daily Bollinger Band Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategy that trades daily Bollinger Band breakouts using a trend filter and ATR-based position sizing.

## Details

- **Entry Criteria**: Price crosses the upper band with positive slopes for long or the lower band with negative slopes for short.
- **Exit Criteria**: Close the position when price crosses the middle band.
