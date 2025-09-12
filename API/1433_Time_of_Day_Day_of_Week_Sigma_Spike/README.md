# Time of day / day of week Sigma Spike Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses return z-score to highlight large moves by hour and optional day filters.
Buys on spikes and exits when volatility normalizes.

## Details

- **Entry Criteria**: absolute z-score >= `Threshold`
- **Long/Short**: Long only
- **Exit Criteria**: z-score falls below `Threshold`
- **Stops**: No
- **Default Values**:
  - `Threshold` = 2.5
  - `AllDays` = false
  - `DayOfWeekFilter` = Monday
  - `StdevLength` = 20
- **Filters**:
  - Category: Volatility
  - Direction: Long
  - Indicators: StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
