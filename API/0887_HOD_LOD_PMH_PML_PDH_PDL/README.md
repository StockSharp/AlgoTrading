# HOD/LOD/PMH/PML/PDH/PDL Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades breakouts of premarket and previous day levels.
Long entries occur when price crosses above the premarket or previous day high.
Short entries occur when price crosses below the premarket or previous day low.
Positions close when price reaches the current day's high or low.

## Details

- **Entry Criteria**: price crossing premarket or previous day levels
- **Long/Short**: Both
- **Exit Criteria**: reach current day's high or low
- **Stops**: No
- **Default Values**:
  - `CandleType` = 5 minutes
- **Filters**:
  - Category: Levels
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
