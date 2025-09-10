# Adaptive Trend Flow Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Adaptive Trend Flow strategy builds a volatility-based channel from fast and slow EMAs of the typical price. When price crosses the channel boundaries the internal trend flips. Long positions are opened when the trend turns upward and optional SMA and MACD filters agree. Positions are closed when the trend reverses downward.

## Details

- **Entry Criteria**:
  - Trend changes from down to up and filters confirm.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Trend changes from up to down.
- **Stops**: None.
- **Default Values**:
  - `Length` = 2
  - `SmoothLength` = 2
  - `Sensitivity` = 2.0
  - `UseSmaFilter` = true
  - `SmaLength` = 4
  - `UseMacdFilter` = true
  - `MacdFastLength` = 2
  - `MacdSlowLength` = 7
  - `MacdSignalLength` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA, SMA, MACD, Standard Deviation
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
