# Hulk Grid Algorithm V2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Grid trading strategy that places ten layered buy limit orders around a user-defined mid price. Orders increase in size closer to the mid level. The strategy exits all positions and cancels remaining orders when price hits a stop-loss below the lowest grid or a take-profit above the upper grid.

## Details

- **Entry Criteria**: Grid of ten limit buy orders from the lowest to highest level.
- **Long/Short**: Long only.
- **Exit Criteria**: Stop-loss under lowest grid or take-profit above upper grid.
- **Stops**: Percentage-based stop-loss and take-profit.
- **Default Values**:
  - `MidPrice` = 0
  - `StopLossPercent` = 2.0
  - `TakeProfitPercent` = 2.0
  - `GridStep` = 200
  - `Lot` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Grid
  - Direction: Long
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
