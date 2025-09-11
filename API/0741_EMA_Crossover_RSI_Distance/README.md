# EMA Crossover with RSI and Distance Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses multiple EMAs and RSI to generate long and short signals, checking the distance between fast EMAs to confirm trend strength.

## Details

- **Entry Criteria**:
  - EMA5 above EMA13.
  - EMA40 above EMA55.
  - RSI above 50 and above its SMA.
  - Distance between EMA5 and EMA13 above its average and EMA40-EMA13 distance increasing.
  - Close price above EMA5.
- **Long/Short**: Long and short.
- **Exit Criteria**:
  - Signal changes to neutral or opposite direction.
- **Stops**: No.
- **Default Values**:
  - `EmaShortLength` = 5
  - `EmaMediumLength` = 13
  - `EmaLong1Length` = 40
  - `EmaLong2Length` = 55
  - `RsiLength` = 14
  - `RsiAverageLength` = 14
  - `DistanceLength` = 5
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, RSI
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
