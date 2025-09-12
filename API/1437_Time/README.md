# Time
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy illustrates timing utilities. It buys when the high exceeds the open by a number of ticks for a specified duration.

## Details

- **Entry Criteria**: High minus open stays above threshold for given seconds.
- **Long/Short**: Long only.
- **Exit Criteria**: Condition fails.
- **Stops**: No.
- **Default Values**:
  - `TicksFromOpen` = 0
  - `SecondsCondition` = 20
  - `ResetOnNewBar` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: Price
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
