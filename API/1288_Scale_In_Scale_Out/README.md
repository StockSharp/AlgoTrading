# Scale In Scale Out Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy gradually builds a position by investing a fixed percentage of available cash on each bar. When the position value reaches a configurable profit level, it sells a portion of the position and optionally keeps part of the realised profit aside.

## Details

- **Entry Criteria**: Always buy when cash is available.
- **Exit Criteria**: Sell when profit percentage exceeds the threshold.
- **Long/Short**: Long only.
- **Default Values**:
  - `Buy Scaling Size %` = 2
  - `Take Profit Level %` = 50
  - `Take Profit Size %` = 1
  - `Retain Profit Portion %` = 50
  - `Minimum Position Value` = 200000
  - `Minimum Buy Value` = 100
- **Filters**:
  - Category: Other
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
