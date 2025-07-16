# Spring Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Spring Reversal is a Wyckoff concept where price briefly breaks support and then springs back above it.
This shakeout traps late sellers and often marks the beginning of an uptrend.

The strategy buys once price reclaims the broken level, anticipating swift short covering and new demand.

A stop just below the spring low limits downside, and the position is closed if follow-through fails.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: Wyckoff
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
