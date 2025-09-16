# Instantaneous Trend Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses John Ehlers' Instantaneous Trendline and a trigger line to generate signals on any timeframe. The trigger is computed as `2 * ITrend - ITrend[2]`, forming a fast line that crosses the slower trendline. A downward crossover closes short positions and opens a long, while an upward crossover closes longs and opens a short. The smoothing factor `Alpha` controls responsiveness: lower values produce smoother lines, higher values react faster.

## Details

- **Entry Criteria**:
  - **Long**: Trigger was above the trendline on the previous bar and crosses below it on the current bar.
  - **Short**: Trigger was below the trendline on the previous bar and crosses above it on the current bar.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Long positions are closed when a short signal appears.
  - Short positions are closed when a long signal appears.
- **Stops**: None by default.
- **Default Values**:
  - `Alpha` = 0.07.
  - `Candle Type` = 4-hour timeframe.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Single
  - Stops: No
  - Complexity: Simple
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
