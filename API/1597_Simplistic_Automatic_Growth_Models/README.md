# Simplistic Automatic Growth Models Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy forms cumulative average high and low bands and trades when price breaks those levels.

## Details

- **Entry Criteria**:
  - Close price above the upper band opens a long.
  - Close price below the lower band opens a short.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal closes the position.
- **Stops**: None.
- **Default Values**:
  - `Length` = 10
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
