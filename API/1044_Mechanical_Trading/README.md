# Mechanical Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A time-based mechanical strategy that executes one trade at a specified hour each day. The position direction can be configured to go long or short. The trade is automatically protected with percent-based take profit and stop loss levels.

## Details

- **Entry Criteria**:
  - **Long**: At `TradeHour` when `Short Mode` is disabled.
  - **Short**: At `TradeHour` when `Short Mode` is enabled.
- **Long/Short**: Both, depending on `Short Mode`.
- **Exit Criteria**:
  - `Profit Target (%)` above/below entry.
  - `Stop Loss (%)` below/above entry.
- **Stops**: Both stop loss and take profit.
- **Default Values**:
  - `Profit Target (%)` = 0.4.
  - `Stop Loss (%)` = 0.2.
  - `Trade Hour` = 16.
- **Filters**:
  - Category: Time
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
