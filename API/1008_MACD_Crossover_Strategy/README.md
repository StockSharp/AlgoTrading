# MACD Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on MACD crossover within specified zone.

MACD Crossover Strategy waits for the MACD line to cross the signal line while the MACD value stays between lower and upper thresholds. Opposite crossover closes the existing position. No stop-loss is applied.

## Details

- **Entry Criteria**: MACD crossover within zone.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `LowerThreshold` = -0.5m
  - `UpperThreshold` = 0.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: MACD
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
