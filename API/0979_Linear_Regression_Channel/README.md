# Linear Regression Channel
[Русский](README_ru.md) | [中文](README_cn.md)

Trades pullbacks when price moves outside a linear regression channel and reverts toward the mean.

## Details

- **Data**: Price candles.
- **Entry**: Buy when close drops below lower band in an uptrend; sell when close rises above upper band in a downtrend.
- **Exit**: Close when price returns to the regression line.
- **Instruments**: Any instruments.
- **Risk**: Channel boundaries act as dynamic limits.
