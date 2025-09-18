# H4L4 Breakout
[Русский](README_ru.md) | [中文](README_cn.md)

Daily breakout strategy that calculates H4 and L4 levels from the previous day's high, low, and close.
A sell limit is placed at H4 and a buy limit at L4 at the start of each day.
All open positions and pending orders are cleared before new orders are submitted.
Protective stop loss and take profit are applied using tick-based distances.

## Details

- **Entry Criteria**: Sell limit at H4 and buy limit at L4 derived from the previous day's candle.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop loss or take profit.
- **Stops**: Yes.
- **Default Values**:
  - `TakeProfit` = 57
  - `StopLoss` = 7
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
