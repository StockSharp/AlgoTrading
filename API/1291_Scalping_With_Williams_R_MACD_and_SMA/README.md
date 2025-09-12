# Scalping With Williams R MACD and SMA
[Русский](README_ru.md) | [中文](README_cn.md)

Scalping strategy using Williams %R, MACD histogram and a simple moving average on one-minute candles.

## Details

- **Entry Criteria**: Williams %R crosses activation levels and MACD histogram changes sign in trend direction.
- **Long/Short**: Both directions.
- **Exit Criteria**: Histogram reverses direction.
- **Stops**: No.
- **Default Values**:
  - `WilliamsLength` = 140
  - `MacdFast` = 24
  - `MacdSlow` = 52
  - `MacdSignal` = 9
  - `SmaLength` = 7
