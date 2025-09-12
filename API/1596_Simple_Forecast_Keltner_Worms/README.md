# Simple Forecast - Keltner Worms Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy builds a dynamic Keltner channel and trades when price moves outside the band.

## Details

- **Entry Criteria**:
  - Close price above the upper channel opens a long.
  - Close price below the lower channel opens a short.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal closes the position.
- **Stops**: None.
- **Default Values**:
  - `Length` = 10
- **Filters**:
  - Category: Channel
  - Direction: Both
  - Indicators: EMA, ATR
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
