# Renko Chart From Ticks Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Generates Renko bricks directly from ticks and trades when brick direction changes. Demonstrates building non-time-based candles using the high level StockSharp API.

## Details

- **Entry Criteria**:
  - When a new finished brick reverses direction, enter in direction of the new brick.
- **Long/Short**: Both.
- **Exit Criteria**: Reverse position on opposite brick direction.
- **Stops**: No.
- **Default Values**:
  - `BrickSize` = 10
  - `Volume` = 1
- **Filters**:
  - Category: Renko
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Tick-based
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
