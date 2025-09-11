# Pivot Point SuperTrend TrendFilter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines a pivot-based SuperTrend line with a SuperTrend trend filter and a moving average confirmation. Trades when the trend flips or when a pivot SuperTrend signal appears within a date window.

## Details

- **Entry Criteria**:
  - Trend filter flips up and price is above the moving average.
  - Pivot SuperTrend emits a buy signal within the configured date range.
- **Exit Criteria**:
  - Trend filter flips down or pivot SuperTrend emits a sell signal.
- **Stops**: None
- **Default Values**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Pivot, SuperTrend, SMA
  - Stops: No
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: Optional
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
