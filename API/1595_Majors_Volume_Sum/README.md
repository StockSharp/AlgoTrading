# Majors Volume Sum Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy sums signed volume over recent candles and trades when the short-term sum exceeds a fraction of its historical maximum.

## Details

- **Entry Criteria**:
  - 10-period signed volume sum is above `Threshold` × maximum and no position, enter long.
  - 10-period signed volume sum is below `-Threshold` × maximum and no position, enter short.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite signal closes the position.
- **Stops**: None.
- **Default Values**:
  - `Threshold` = 0.75
- **Filters**:
  - Category: Volume
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
