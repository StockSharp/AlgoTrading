# My Line Order
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy triggers market orders when price crosses predefined horizontal levels. The user specifies separate levels for long and short entries and risk parameters in pips. After opening a position the strategy tracks stop loss, take profit and optional trailing stop.

The system suits discretionary setups where entry levels are known in advance. It works on any instrument and timeframe because it relies only on price levels.

## Details

- **Entry Criteria**:
  - **Long**: Close price crosses above `BuyPrice`.
  - **Short**: Close price crosses below `SellPrice`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Stop-loss at `StopLossPips`.
  - Take-profit at `TakeProfitPips`.
  - Trailing stop if `TrailingStopPips` > 0.
- **Stops**: Yes, in pips.
- **Default Values**:
  - `BuyPrice` = 0 (disabled)
  - `SellPrice` = 0 (disabled)
  - `TakeProfitPips` = 30
  - `StopLossPips` = 20
  - `TrailingStopPips` = 0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Manual
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
