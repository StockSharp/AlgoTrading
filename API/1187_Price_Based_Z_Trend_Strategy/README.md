# Price Based Z-Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades based on the price Z-score relative to an EMA. Enters when the Z-score crosses user-defined thresholds and supports long, short, or both directions.

## Details

- **Entry Criteria**:
  - Z-score crosses above `Threshold` for long.
  - Z-score crosses below `-Threshold` for short.
- **Long/Short**: Configurable via `TradeDirection`.
- **Exit Criteria**: Opposite threshold crossover.
- **Stops**: No.
- **Default Values**:
  - `PriceDeviationLength` = 100
  - `PriceAverageLength` = 100
  - `Threshold` = 1
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Configurable
  - Indicators: EMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: 5-minute
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
