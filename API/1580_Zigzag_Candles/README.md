# Zigzag Candles
[Русский](README_ru.md) | [中文](README_cn.md)

Simple strategy that reacts to ZigZag pivot points. A long position is opened when a new low pivot forms, while a short position is taken at new high pivots.

## Details
- **Entry Criteria**: Pivot highs and lows from ZigZag.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite pivot.
- **Stops**: No.
- **Default Values**:
  - `ZigzagLength` = 5
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
