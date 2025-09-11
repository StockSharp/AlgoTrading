# The Z-Score Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy calculates the z-score of a Heikin-Ashi EMA and trades based on crossings of dynamic thresholds derived from recent ranges.

## Details

- **Entry Criteria**: Score crossing above recent low or EMA score crossing above mid range
- **Long/Short**: Both
- **Exit Criteria**: Score EMA crossing below recent high or low
- **Stops**: No
- **Default Values**:
  - `HaEmaLength` = 10
  - `ScoreLength` = 25
  - `ScoreEmaLength` = 20
  - `RangeWindow` = 100
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: EMA, SMA, StdDev, Highest, Lowest
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
