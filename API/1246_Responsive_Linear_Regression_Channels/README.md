# Responsive Linear Regression Channels
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptive linear regression channel that adjusts lookback to timeframe and trades pullbacks.

## Details

- **Data**: Price candles.
- **Entry**: Buy when price drops below lower band in uptrend; sell when price rises above upper band in downtrend.
- **Exit**: Close when price returns to regression line.
- **Instruments**: Any.
- **Risk**: Channel width controls exposure.
