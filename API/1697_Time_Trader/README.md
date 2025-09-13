# Time Trader Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Time-based strategy that enters long and/or short exactly at a specified clock time and protects the position with configurable take profit and stop loss.

## Details

- **Entry Criteria**: At `TradeHour:TradeMinute:TradeSecond` open long if `AllowBuy`, short if `AllowSell`.
- **Long/Short**: Both, depending on settings
- **Exit Criteria**: position closed via stop loss or take profit
- **Stops**: Yes, both
- **Default Values**:
  - `Volume` = 1
  - `TakeProfit` = 0.2
  - `StopLoss` = 0.2
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `TradeSecond` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `CandleType` = TimeSpan.FromSeconds(1).TimeFrame()
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
  - Risk level: Medium

