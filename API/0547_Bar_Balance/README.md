# Bar Balance Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy measures the balance between upward and downward moves inside each candle. A positive balance suggests buyers dominate the bar, while a negative balance points to selling pressure.

The system smooths this balance with a moving average. When both the current balance and its average are above zero, the strategy enters a long position. When both drop below zero, it enters short.

## Details

- **Entry criteria**: balance > 0 and average > 0 for long; balance < 0 and average < 0 for short.
- **Exit criteria**: opposite signal triggers position reversal.
- **Indicators**: custom bar balance, SMA.
- **Long/Short**: both.
- **Stop-loss**: none.
