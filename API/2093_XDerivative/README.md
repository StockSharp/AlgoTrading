# XDerivative Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

XDerivative Strategy tracks shifts in price momentum using a smoothed rate of change. The original MQL expert combines a rate-of-change calculation with Jurik smoothing to detect turning points. The StockSharp version reuses built-in indicators to implement the same concept.

The strategy computes the rate of change over `RocPeriod` bars and smooths it with a Jurik Moving Average of length `MaLength`. When the smoothed derivative forms a trough (previous value is lower than its predecessor and the current value rises above the previous) the strategy enters or flips to a long position. When a peak forms (previous value is higher than its predecessor and the current value falls below it) the strategy enters or flips to a short position. Protective stops manage exits.

## Details

- **Entry Criteria**:
  - Long: Smoothed derivative turns up after a local minimum.
  - Short: Smoothed derivative turns down after a local maximum.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite derivative turn or protective stop.
- **Stops**: Yes, percentage take-profit and stop-loss.
- **Default Values**:
  - `RocPeriod` = 34
  - `MaLength` = 7
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 1
  - `CandleType` = TimeSpan.FromHours(4)
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: RateOfChange, JurikMovingAverage
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: 4H
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
