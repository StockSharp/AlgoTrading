# Stop Level Counter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Utility strategy that measures potential profit for buy and sell orders at a user-defined price level.
It subscribes to Level1 quotes, computes profit for both directions based on the difference between the chosen level and current bid/ask, and logs the values.
A horizontal line is drawn on the chart to visualize the level.

## Details

- **Entry Criteria**: None
- **Long/Short**: Both (profit estimated for long and short)
- **Exit Criteria**: None
- **Stops**: No
- **Default Values**:
  - `Level` = 0 (initialized to current bid at start)
  - `Volume` = 1
- **Filters**:
  - Category: Utility
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
