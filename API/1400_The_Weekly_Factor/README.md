# The Weekly Factor Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements the Weekly Factor pattern described by Andrea Unger. The strategy trades breakouts of the session high or low when the five-day range shows compression.

## Details
- **Entry Criteria**: After session start, if Weekly Factor condition true and price breaks session high -> long, breaks session low -> short.
- **Long/Short**: Both.
- **Exit Criteria**: Close at new session or after two days of profitable position.
- **Stops**: None.
- **Default Values**:
  - `RangeFilter` = 0.5
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Weekly factor
  - Stops: No
  - Complexity: Medium
  - Timeframe: 15m
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
