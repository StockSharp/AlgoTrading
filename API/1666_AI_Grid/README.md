# AI Grid Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

AI Grid Strategy places layered buy and sell orders around the current price. The strategy supports breakout (stop) and counter-trend (limit) approaches. After an order is filled, a take-profit order is automatically placed.

## Details

- **Entry Criteria**: Price reaches one of the grid levels.
- **Long/Short**: Controlled via `AllowLong` and `AllowShort`.
- **Exit Criteria**: Take profit after fixed distance `TakeProfit`.
- **Stops**: No stop-loss.
- **Default Values**:
  - `GridSize` = 50m
  - `GridSteps` = 10
  - `TakeProfit` = 50m
  - `AllowLong` = true
  - `AllowShort` = true
  - `UseBreakout` = true
  - `UseCounter` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Grid
  - Direction: Both
  - Indicators: None
  - Stops: Take Profit only
  - Complexity: Intermediate
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
