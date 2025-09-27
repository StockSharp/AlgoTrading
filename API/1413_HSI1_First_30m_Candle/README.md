# HSI1 First 30m Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades breakouts of the first 30-minute range on a 15-minute chart, allowing only one trade per day.

## Details

- **Entry Criteria**: Price breaks above/below the first 30-minute high/low during session.
- **Long/Short**: Both directions, selectable.
- **Exit Criteria**: Take profit or stop loss based on range.
- **Stops**: Yes.
- **Default Values**:
  - `RiskReward` = 1
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Price
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
