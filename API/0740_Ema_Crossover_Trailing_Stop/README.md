# EMA Crossover Trailing Stop Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **EMA Crossover Trailing Stop Strategy** opens a long position when the short EMA crosses above the long EMA and opens a short position when it crosses below. A trailing stop based on the highest or lowest price after entry closes the position when price reverses by a set percentage.

## Details
- **Entry Criteria**: short EMA crossing long EMA.
- **Long/Short**: both directions.
- **Exit Criteria**: opposite crossover or trailing stop.
- **Stops**: trailing stop using highest/lowest price since entry.
- **Default Values**:
  - `ShortLength = 9`
  - `LongLength = 21`
  - `TrailStopPercent = 1`
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA
  - Stops: Trailing stop
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
