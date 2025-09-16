# Time Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy opens long and/or short positions at a specific time of day with predefined stop loss and take profit distances. It is useful for testing time-based entries without any indicator confirmation.

## Details

- **Entry Criteria**: Time-based trigger at configured hour and minute.
- **Long/Short**: Both directions (configurable).
- **Exit Criteria**: Protective stop or target.
- **Stops**: Yes.
- **Default Values**:
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `TakeProfitTicks` = 20
  - `StopLossTicks` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Other
  - Direction: Both
  - Indicators: None
  - Stops: Fixed
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
