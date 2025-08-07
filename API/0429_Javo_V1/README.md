# Javo v1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Javo v1 combines Heikin Ashi candles with a pair of exponential moving averages. A position is opened when the HA direction and the fast/slow EMA crossover align. The approach attempts to catch emerging trends while smoothing noise.

## Details

- **Entry Criteria**:
  - **Long**: HA bullish and `EMA_fast > EMA_slow`
  - **Short**: HA bearish and `EMA_fast < EMA_slow`
- **Long/Short**: Both sides
- **Exit Criteria**: Opposite signal
- **Stops**: None
- **Default Values**:
  - `FastEmaPeriod` = 1
  - `SlowEmaPeriod` = 30
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Heikin Ashi, EMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Hourly
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
