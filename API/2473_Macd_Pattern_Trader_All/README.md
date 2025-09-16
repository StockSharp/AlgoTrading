# Macd Pattern Trader All
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that opens positions on sharp MACD reversals. It looks for two large spikes around a small intermediate value of the MACD line. A sell is opened when the previous MACD value is positive and the current value drops deep into negative territory. A buy is opened on the opposite condition. Stop loss and take profit are derived from recent highs and lows.

The algorithm suits volatile markets where momentum quickly changes direction. It uses only market orders and calculates risk levels from candle history.

## Details

- **Entry Criteria**: MACD spike ratio based on `RatioThreshold`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop at recent extreme plus offset or opposite spike.
- **Stops**: Yes.
- **Default Values**:
  - `FastEmaPeriod` = 24
  - `SlowEmaPeriod` = 13
  - `StopLossBars` = 22
  - `TakeProfitBars` = 32
  - `OffsetPoints` = 40
  - `RatioThreshold` = 5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
