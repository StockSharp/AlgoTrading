# Resampling Reverse Engineering Bands
[Русский](README_ru.md) | [中文](README_cn.md)

Resampling Reverse Engineering Bands reverse engineers RSI price levels at a configurable resampling rate. The strategy buys when price falls below the low band and sells when price rises above the high band.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Close price crosses below the low RRSI band.
  - **Short**: Close price crosses above the high RRSI band.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `RsiPeriod` = 14
  - `HighThreshold` = 70
  - `LowThreshold` = 30
  - `SampleLength` = 1
- **Filters**:
  - Category: Momentum
  - Direction: Long & Short
  - Indicators: RSI
  - Complexity: Medium
  - Risk level: Medium
