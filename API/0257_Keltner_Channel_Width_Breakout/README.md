# Keltner Channel Width Breakout
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Keltner Channel Width Breakout strategy observes the Keltner for rapid expansions. When readings jump beyond their typical range, price often starts a new move.

A position opens once the indicator pierces a band derived from recent data and a deviation multiplier. Long and short trades are possible with a stop attached.

This system fits momentum traders seeking early breakouts. Trades close as the Keltner falls back toward the mean. Defaults start with `EMAPeriod` = 20.

## Details

- **Entry Criteria**: Indicator exceeds average by deviation multiplier.
- **Long/Short**: Both directions.
- **Exit Criteria**: Indicator reverts to average.
- **Stops**: Yes.
- **Default Values**:
  - `EMAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopMultiplier` = 2
- **Filters**:
  - Category: Breakout
  - Direction: Both
  - Indicators: Keltner
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
