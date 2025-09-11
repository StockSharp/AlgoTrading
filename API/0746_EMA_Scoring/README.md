# EMA Scoring Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy evaluates market direction using three EMA lines and trades when a score threshold is crossed.

## Details
- **Entry Criteria**:
  - **Long**: Score crosses above threshold.
  - **Short**: Score crosses below negative threshold.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal.
- **Stops**: No.
- **Default Values**:
  - `Short EMA Period` = 21
  - `Medium EMA Period` = 50
  - `Long EMA Period` = 100
  - `Score Threshold` = 4
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Medium-term
