# MACD Trend Mode Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that trades using the MACD indicator with three selectable trend detection modes: histogram slope, cloud crossover, or zero line cross.

## Details

- **Entry Criteria**:
  - *Histogram*: histogram was falling then turns upward for longs; rising then turns downward for shorts.
  - *Cloud*: MACD line previously above signal line and crosses below triggers long; opposite cross triggers short.
  - *Zero*: histogram crosses the zero line in opposite direction.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite conditions close positions.
- **Stops**: No.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TrendMode` = TrendMode.Cloud
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: 4h
  - Seasonality: No
  - Neural Networks: No
  - Divergence: Yes (histogram)
  - Risk Level: Medium
