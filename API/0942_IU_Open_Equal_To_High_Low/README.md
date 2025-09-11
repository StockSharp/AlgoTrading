# IU Open Equal to High Low Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Enters long on the first candle of the day when its open equals its low and enters short when the open equals the high. Stop loss uses the prior candle and take profit is based on the `RiskReward` ratio.

## Details

- **Entry Criteria**:
  - **Long**: first candle's open equals its low.
  - **Short**: first candle's open equals its high.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Stop loss at the previous candle low for long, previous candle high for short.
  - Take profit calculated from entry price using `RiskReward`.
- **Stops**: Yes.
- **Default Values**:
  - `RiskReward` = 2.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Price action
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
