# Breakdown Level Day Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Places breakout orders around the previous day's high and low at a specified time. Enters long when price crosses above the high plus a delta and short when price drops below the low minus the delta. Position management includes optional stop loss, take profit, break-even and trailing stop.

## Details

- **Entry Criteria**:
  - Long: price crosses above previous day's high + `Delta`
  - Short: price crosses below previous day's low − `Delta`
- **Long/Short**: Both
- **Exit Criteria**:
  - Stop loss or take profit reached
  - Trailing stop or break-even adjustment triggers
- **Stops**: Points from entry price
- **Default Values**:
  - `OrderTime` = TimeSpan.Zero
  - `Delta` = 6
  - `StopLoss` = 120
  - `TakeProfit` = 90
  - `NoLoss` = 0
  - `Trailing` = 0
  - `Volume` = 1m
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
