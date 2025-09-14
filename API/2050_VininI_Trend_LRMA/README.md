# VininI Trend LRMA Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

VininI Trend LRMA Strategy uses a Linear Regression Moving Average (LRMA) to track market direction. The strategy supports two entry modes:
- **Breakdown**: trades when LRMA crosses fixed upper or lower levels.
- **Twist**: trades when LRMA reverses direction.

## Details

- **Entry Criteria**: LRMA crosses levels (Breakdown) or changes direction (Twist)
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: None
- **Default Values**:
  - `CandleType` = TimeFrameCandle 4h
  - `Period` = 13
  - `UpLevel` = 10
  - `DnLevel` = -10
  - `Mode` = Breakdown
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: LinearRegression
  - Stops: None
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
