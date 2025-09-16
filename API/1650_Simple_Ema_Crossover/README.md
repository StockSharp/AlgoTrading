# Simple EMA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses a crossover of two exponential moving averages with built-in stop-loss and take-profit.

It buys when the fast EMA crosses above the slow EMA and sells when it crosses below.

## Details

- **Entry Criteria**: Fast EMA crossing slow EMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover or stop orders.
- **Stops**: Yes.
- **Default Values**:
  - `Periods` = 17
  - `StopLoss` = 31 (absolute)
  - `TakeProfit` = 69 (absolute)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
