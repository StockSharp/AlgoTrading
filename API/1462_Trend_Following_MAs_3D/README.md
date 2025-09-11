# Trend Following MAs 3D Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses two short simple moving averages to detect trend direction.
A long position is opened when the 5-period average is above the 10-period average.
A short position is opened when the opposite occurs.

## Details

- **Entry**:
  - **Long**: SMA(5) > SMA(10)
  - **Short**: SMA(5) < SMA(10)
- **Exit**: reverse signal
- **Indicators**: SMA
- **Timeframe**: configurable
- **Type**: Trend following
