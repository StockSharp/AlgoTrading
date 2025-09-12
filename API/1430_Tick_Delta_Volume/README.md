# Tick Delta Volume Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Analyzes per-tick changes in price and volume. The delta is compared against its moving average and standard deviation to generate simple momentum-based entries.

## Details

- **Entry Criteria**: delta > mean + stdev for long, delta < -(mean + stdev) for short
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `Mode` = Volume
  - `Length` = 10
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: EMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Tick
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
