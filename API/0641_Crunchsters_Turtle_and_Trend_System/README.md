# Crunchster's Turtle and Trend System Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines a fast/slow EMA trend filter with Donchian channel breakout entries and ATR based stop management. A trailing Donchian channel exits positions when momentum reverses.

## Details

- **Entry Criteria**: EMA differential cross or Donchian breakout.
- **Long/Short**: Both.
- **Exit Criteria**: Trailing channel or ATR stop.
- **Stops**: Yes, ATR based.
- **Default Values**:
  - `CandleType` = 1 hour
  - `FastEmaPeriod` = 10
  - `BreakoutPeriod` = 20
  - `TrailPeriod` = 1000
  - `StopAtrMultiple` = 20
  - `OrderPercent` = 10
  - `TrendEnabled` = true
  - `BreakoutEnabled` = false
- **Filters**:
  - Category: Trend
  - Direction: Long & Short
  - Indicators: EMA, Donchian, ATR
  - Stops: Yes
  - Complexity: Medium
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
