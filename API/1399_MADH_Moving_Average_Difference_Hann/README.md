# MADH Moving Average Difference, Hann Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Implements the MADH indicator described by John Ehlers. Strategy goes long when the indicator is above zero and short when below.

## Details
- **Entry Criteria**: MADH > 0 for longs, MADH < 0 for shorts.
- **Long/Short**: Both.
- **Exit Criteria**: Reverse on opposite signal.
- **Stops**: None.
- **Default Values**:
  - `ShortLength` = 8
  - `DominantCycle` = 27
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: MADH
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
