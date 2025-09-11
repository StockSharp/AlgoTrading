# Multi-Step FlexiSuperTrend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A SuperTrend filter combined with a smoothed deviation oscillator.
The strategy includes three configurable take-profit levels.

## Details

- **Entry Criteria**:
  - Price below SuperTrend and deviation (SMA of price minus SuperTrend) > 0 → buy.
  - Price above SuperTrend and deviation < 0 → sell.
- **Long/Short**: Long, short or both directions.
- **Exit Criteria**:
  - Partial take profit at 3 levels.
  - Remaining position closed on trend reversal when price crosses SuperTrend.
- **Stops**: No stop logic by default.
- **Default Values**:
  - ATR period = 10.
  - ATR factor = 3.0.
  - SMA length = 10.
  - Take profit levels = 2%, 8%, 18%.
  - Take profit percents = 30%, 20%, 15%.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SuperTrend, SMA
  - Stops: Take profit
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
