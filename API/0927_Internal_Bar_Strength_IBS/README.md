# Internal Bar Strength IBS Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when the internal bar strength (IBS) is below a lower threshold and exits when IBS rises above an upper threshold within a specified time window.

## Details

- **Entry Criteria**:
  - IBS < `LowerThreshold`.
  - Time between `StartTime` and `EndTime`.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - IBS >= `UpperThreshold`.
- **Stops**: None.
- **Default Values**:
  - `UpperThreshold` = 0.8
  - `LowerThreshold` = 0.2
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
