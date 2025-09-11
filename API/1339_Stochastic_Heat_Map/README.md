# Stochastic Heat Map Strategy
[Русский](README_ru.md) | [中文](README_zh.md)

The Stochastic Heat Map strategy averages a set of stochastic oscillators with rising periods.
The combined reading is smoothed again to form a fast and a slow line.
Trades go long when the fast line crosses above the slow line and go short on the opposite cross.

## Details

- **Entry Criteria**: fast/slow crossover
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: None
- **Default Values**:
  - `CandleType` = 15 minute
  - `Increment` = 10
  - `SmoothFast` = 2
  - `SmoothSlow` = 21
  - `PlotNumber` = 28
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Stochastic
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
