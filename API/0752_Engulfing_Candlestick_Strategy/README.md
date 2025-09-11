# Engulfing Candlestick Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades on a selected engulfing pattern. Choose either bullish or bearish engulfing and a trade side to open when the pattern appears. The position is held for a fixed number of bars before being closed.

## Details

- **Entry Criteria**: Selected engulfing pattern (bullish or bearish).
- **Long/Short**: Configurable long or short.
- **Exit Criteria**: Position closed after the specified number of bars.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = 15 minute
  - `HoldPeriods` = 17
  - `Pattern` = Bullish
  - `Side` = Long
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
