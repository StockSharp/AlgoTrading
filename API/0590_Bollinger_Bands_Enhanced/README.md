# Bollinger Bands Enhanced Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Buys when price dips below the lower Bollinger Band while the market stays above a 200-period EMA.  
A stop loss is placed at `entry - ATR * stop`, and after price rises `ATR * trail` above entry, the middle band becomes a trailing target.

## Details

- **Entry Criteria**: `Low > EMA` and `Low <= Lower Band`.
- **Long/Short**: Long only.
- **Exit Criteria**: Close below middle band after trailing activates or low below stop.
- **Stops**: ATR-based stop loss.
- **Default Values**:
  - Bollinger period = 20
  - EMA period = 200
  - ATR period = 14
  - Stop ATR = 1.75
  - Trail ATR = 2.25

