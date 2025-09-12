# Z-Strike Recovery Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Enters long when the z-score of price change exceeds a threshold and exits after a fixed number of bars.

## Details

- **Entry Criteria**: Z-score of price change > threshold
- **Long/Short**: Long only
- **Exit Criteria**: Time-based exit
- **Stops**: No
- **Default Values**:
  - `ZLength` = 16
  - `ZThreshold` = 1.3
  - `ExitPeriods` = 10
- **Filters**:
  - Category: Statistical
  - Direction: Long
  - Indicators: SMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
