# Source Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Source Strategy enters long when the candle closes above its open and short when it closes below. Optional stop loss, take profit, and trailing stop percentages manage the open position.

## Details

- **Entry Criteria**: long when close > open, short when close < open
- **Long/Short**: Both
- **Exit Criteria**: opposite signal or triggered stop management
- **Stops**: Optional stop loss, take profit, trailing stop
- **Default Values**:
  - `SL %` = 1
  - `TP %` = 3
  - `Trail Points %` = 3
  - `Trail Offset %` = 1
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: None
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
