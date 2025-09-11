# HSI First 30m Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy captures the high and low of the first 30 minutes after the Hong Kong session opens and trades breakouts on a 5-minute chart. Only one trade is allowed per day.

## Details

- **Entry Criteria**:
  - **Long**: price breaks above the first 30-minute high during the session.
  - **Short**: price falls below the first 30-minute low during the session.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Stop loss at the opposite side of the range.
  - Take profit at range size multiplied by `RiskReward` from the entry.
- **Stops**: Yes.
- **Default Values**:
  - `RiskReward` = 1.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Price action
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
