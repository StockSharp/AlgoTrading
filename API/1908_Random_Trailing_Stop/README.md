# Random Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Random Trailing Stop Strategy opens random trades biased by a simple moving average and manages them using a trailing stop.

## Details

- **Entry Criteria**: random direction with bias from SMA
- **Long/Short**: Both
- **Exit Criteria**: trailing stop
- **Stops**: Yes
- **Default Values**:
  - `MinStopLevel` = 0.00036
  - `TrailingStep` = 0.00001
  - `SleepMinutes` = 5
  - `SmaPeriod` = 100
  - `Volume` = 0.1
- **Filters**:
  - Category: Experimental
  - Direction: Both
  - Indicators: SMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: 1m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
