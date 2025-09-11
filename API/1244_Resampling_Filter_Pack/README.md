# Resampling Filter Pack Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy samples price every N bars and smooths it with a moving average. It goes long when the filtered value rises and price trades above it, and goes short when the filtered value falls and price is below.

## Details
- **Entry Criteria**:
  - **Long**: Filter slope is up and close is above the filter.
  - **Short**: Filter slope is down and close is below the filter.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `BarsPerSample` = 5
  - `MovingAverageType` = EMA
  - `MaPeriod` = 9
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: Moving Average
  - Complexity: Simple
  - Risk level: Medium
