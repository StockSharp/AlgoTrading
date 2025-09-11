# Forex Hammer and Hanging Man
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades classic candlestick reversal patterns: the bullish hammer and the bearish hanging man. It goes long after a hammer and short after a hanging man, holding the position for a fixed number of bars.

The position is closed once the holding period expires or protective stops are hit.

## Details

- **Entry Criteria**: Hammer for long, hanging man for short.
- **Long/Short**: Both.
- **Exit Criteria**: Hold period or stop-loss/take-profit.
- **Stops**: Yes.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `BodyLengthMultiplier` = 5
  - `ShadowRatio` = 1
  - `HoldPeriods` = 26
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
