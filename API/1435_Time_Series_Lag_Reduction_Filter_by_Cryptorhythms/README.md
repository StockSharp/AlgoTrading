# Time Series Lag Reduction Filter
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on EMA lag reduction filter.

The algorithm compares price with a lag-adjusted EMA and trades on crossovers.

## Details

- **Entry Criteria**: Price crossing lag-reduced EMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `LagReduction` = 20m
  - `EmaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
