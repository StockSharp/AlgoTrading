# Non-Repainting Renko Emulation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Emulates Renko bricks using closing prices and trades on pattern transitions without repainting.

## Details

- **Entry Criteria**:
  - After a new brick forms, go long when previous brick direction and price sequence show upward continuation.
  - Go short on the inverse sequence.
- **Long/Short**: Both.
- **Exit Criteria**: Close positions when brick direction reverses.
- **Stops**: No.
- **Default Values**:
  - `BrickSize` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
