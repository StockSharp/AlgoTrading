# Previous Period Levels - X Alerts
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy tracks the open, high, low and close of the previous period from a higher timeframe. A moving average on the base timeframe generates log messages whenever it crosses these stored levels, similar to the TradingView indicator "Previous Period Levels - X Alerts".

## Details

- **Entry Criteria**: None; logs SMA crosses with previous period levels.
- **Long/Short**: N/A.
- **Exit Criteria**: N/A.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `ReferenceCandleType` = TimeSpan.FromHours(1)
  - `SmaLength` = 3
  - `UseOpen` = true
  - `UseHigh` = true
  - `UseLow` = true
  - `UseClose` = true
- **Filters**:
  - Category: Levels
  - Direction: Neutral
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
