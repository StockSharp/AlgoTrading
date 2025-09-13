# Breakout 04 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades breakouts of the previous day's range.
Buys when price moves above the prior daily high and sells when price falls below the prior daily low.
Uses a trailing stop and fixed take profit with optional position sizing based on account balance.
Trading is disabled before a configured Monday start hour and after a Friday cutoff hour.

## Details

- **Entry Criteria**:
  - Long: `Price > Previous High`
  - Short: `Price < Previous Low`
- **Long/Short**: Both
- **Exit Criteria**: Trailing stop or take profit
- **Stops**: Trailing and fixed stop loss
- **Default Values**:
  - `MondayHour` = 18
  - `FridayHour` = 14
  - `TrailingStop` = 21
  - `TakeProfit` = 550
  - `StopLoss` = 124
  - `UseMoneyManagement` = false
  - `PercentMM` = 8m
  - `Volume` = 0.1m
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
