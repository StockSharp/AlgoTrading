# Supply Demand Engulfment
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades bullish and bearish engulfing patterns near Donchian support and resistance zones.

## Details

- **Entry Criteria**: Engulfing pattern at zone boundaries.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `ZonePeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Donchian
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes (engulfing)
  - Risk Level: Medium
