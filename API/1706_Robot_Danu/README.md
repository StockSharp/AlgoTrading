# Robot Danu
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that compares fast and slow ZigZag levels derived from candle highs and lows.
A short position is opened when the fast ZigZag level is above the slow one.
A long position is opened when the fast ZigZag level is below the slow one.

## Details
- **Entry Criteria**: Comparison of fast and slow ZigZag pivots.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite ZigZag relation.
- **Stops**: None.
- **Default Values**:
  - `FastLength` = 28
  - `SlowLength` = 56
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Highest, Lowest
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
