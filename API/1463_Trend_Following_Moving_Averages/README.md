# Trend Following Moving Averages Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Calculates a moving average and measures its trend inside a dynamic price channel.
Long positions are taken when the trend score is positive and short positions when it is negative.

## Details

- **Entry**:
  - **Long**: trend score > 0
  - **Short**: trend score < 0
- **Exit**: reverse signal
- **Indicators**: SMA, Highest, Lowest
- **Timeframe**: configurable
- **Type**: Trend following
