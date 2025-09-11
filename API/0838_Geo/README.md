# Geo Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that buys when candle high/low ratio is close to the golden ratio.

## Details

- **Entry Criteria**: High/low ratio within tolerance of phi.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite condition.
- **Stops**: No.
- **Default Values**:
  - `Tolerance` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candle ratio
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
