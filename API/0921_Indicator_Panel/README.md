# Indicator Panel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Displays RSI, MACD, DMI, CCI, MFI, Momentum and two moving averages for the current security. The strategy only logs indicator values and does not trade.

## Details

- **Entry Criteria**: None
- **Long/Short**: None
- **Exit Criteria**: None
- **Stops**: None
- **Default Values**:
  - `RsiLength` = 14
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `DiLength` = 14
  - `AdxLength` = 14
  - `CciLength` = 20
  - `MfiLength` = 20
  - `MomentumLength` = 10
  - `Ma1IsEma` = false
  - `Ma1Length` = 50
  - `Ma2IsEma` = false
  - `Ma2Length` = 200
- **Filters**:
  - Category: Informational
  - Direction: None
  - Indicators: RSI, MACD, DMI, CCI, MFI, Momentum, MA
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
