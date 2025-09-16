# Fast2 Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on the Fast2 histogram. The histogram combines the body of the last three candles with square‑root weights and applies two weighted moving averages. A long position is opened when the fast average crosses below the slow one, and a short position when it crosses above.

## Details

- **Entry Criteria**:
  - Long: fast WMA crosses below slow WMA
  - Short: fast WMA crosses above slow WMA
- **Long/Short**: Both
- **Exit Criteria**:
  - Opposite crossover
- **Stops**: None
- **Default Values**:
  - `FastLength` = 3
  - `SlowLength` = 9
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filters**:
  - Category: Crossover
  - Direction: Both
  - Indicators: WeightedMovingAverage
  - Stops: No
  - Complexity: Basic
  - Timeframe: Mid-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
