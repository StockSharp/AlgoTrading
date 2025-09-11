# Engulfing with Trend Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

This strategy combines a SuperTrend filter with bullish and bearish engulfing patterns. A trade is opened when a candle engulfs the prior bar in the direction of the prevailing trend. Stop and target levels are calculated from the pattern range.

## Details

- **Entry Criteria**: Engulfing pattern aligned with SuperTrend direction.
- **Long/Short**: Both.
- **Exit Criteria**: Stop-loss or take-profit.
- **Stops**: Yes, based on candle extremes and ATR offset.
- **Default Values**:
  - `CandleType` = 5 minute
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3
  - `BoringThreshold` = 25
  - `EngulfingThreshold` = 50
  - `StopLevel` = 200
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: SuperTrend, Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
