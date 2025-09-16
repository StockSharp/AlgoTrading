# Heiken Ashi Simplified EA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

A pattern-based system built on Heikin Ashi candles. The strategy watches a sequence of prior Heikin Ashi opens and closes. When three consecutive closes rise (or fall) above their respective opens while the opens form a decelerating pullback, the next candle can trigger a breakout trade once price moves away from the last Heikin Ashi open by a minimum distance. The algorithm scales into positions up to a defined limit.

## Details

- **Entry Criteria**:
  - **Long**: Three previous HA closes are above prior opens and the opens form a decreasing series with shrinking differences.
  - **Short**: Three previous HA closes are below prior opens and the opens form an increasing series with expanding differences.
- **Long/Short**: Both sides
- **Exit Criteria**:
  - Opposite signal
- **Stops**: None
- **Default Values**:
  - `CandleType` = 1 hour
  - `MaxPositions` = 3
  - `DistancePoints` = 300
  - `Volume` = 1
- **Filters**:
  - Category: Pattern breakout
  - Direction: Both
  - Indicators: Heikin Ashi
  - Stops: No
  - Complexity: Medium
  - Timeframe: Hourly
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
