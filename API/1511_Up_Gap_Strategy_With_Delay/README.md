# Up Gap Strategy With Delay
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters long when the session opens with an up gap exceeding a threshold and a specified number of bars has passed since the previous entry. The position is held for a fixed number of bars.

## Details

- **Entry Criteria**: up gap larger than threshold and delay period satisfied
- **Long/Short**: Long
- **Exit Criteria**: after holding period expires
- **Stops**: No
- **Default Values**:
  - `GapThreshold` = 1
  - `DelayPeriods` = 0
  - `HoldingPeriods` = 7
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
