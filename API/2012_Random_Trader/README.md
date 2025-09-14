# Random Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Opens a long or short position randomly when no position is open. Each trade uses fixed take profit and stop loss values measured in price units.

## Details

- **Entry Criteria**: no position and random choice
- **Long/Short**: Both
- **Exit Criteria**: price hits take profit or stop loss
- **Stops**: Yes
- **Default Values**:
  - `Volume` = 1
  - `TakeProfit` = 10
  - `StopLoss` = 10
- **Filters**:
  - Category: Other
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Tick
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: High
