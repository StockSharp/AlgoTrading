[Русский](README_ru.md) | [中文](README_cn.md)

Turtle Trader follows the classic Turtle breakout system using Donchian channels and ATR based money management. It buys when price breaks above recent highs and sells when it falls below recent lows. Pyramiding adds to winning positions as price moves in favor.

## Details

- **Entry Criteria**: breakout of `S1` or `S2` highest high / lowest low
- **Long/Short**: Both
- **Exit Criteria**: opposite breakout or ATR stop
- **Stops**: ATR based
- **Default Values**:
  - `RiskPercent` = 1
  - `AtrPeriod` = 20
  - `StopMultiplier` = 1.5
  - `PyramidProfit` = 0.5
  - `S1Long` = 20
  - `S2Long` = 55
  - `S1LongExit` = 10
  - `S2LongExit` = 20
  - `S1Short` = 15
  - `S2Short` = 55
  - `S1ShortExit` = 7
  - `S2ShortExit` = 20
- **Filters**:
  - Category: Trend Following
  - Direction: Both
  - Indicators: ATR, Highest, Lowest
  - Stops: ATR
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
