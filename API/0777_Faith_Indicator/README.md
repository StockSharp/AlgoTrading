# Faith Indicator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy evaluates market "faith" by measuring volume expansion when price makes higher highs or lower lows. A positive grade suggests buyers dominate, while a negative grade indicates sellers prevail. The strategy trades on transitions between positive and negative grades.

## Details

- **Entry Criteria:** faith grade crosses above zero → buy; crosses below zero → sell.
- **Long/Short:** both.
- **Exit Criteria:** opposite signal.
- **Indicators:** Highest, SMA.
