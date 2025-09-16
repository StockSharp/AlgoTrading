# Opening and Closing on Time Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A simple time-based strategy that opens a market position at a specified time of day and closes it at another predefined time. The direction (buy or sell) and order volume are configurable. This example demonstrates scheduled trade execution without using indicators or additional filters.

## Details

- **Entry Criteria**:
  - **Long**: At `Open Time` when `Is Buy` is enabled.
  - **Short**: At `Open Time` when `Is Buy` is disabled.
- **Long/Short**: Both, depending on `Is Buy`.
- **Exit Criteria**:
  - Position is closed at `Close Time` regardless of profit or loss.
- **Stops**: None.
- **Default Values**:
  - `Open Time` = 13:00.
  - `Close Time` = 13:01.
  - `Volume` = 1.
  - `Is Buy` = true.
  - `Candle Type` = 1 minute.
- **Filters**:
  - Category: Time
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
