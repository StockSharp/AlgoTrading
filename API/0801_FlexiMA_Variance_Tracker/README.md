# FlexiMA Variance Tracker Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Tracks price deviation around a moving average and opens trades when the deviation exceeds a volatility threshold while SuperTrend direction confirms.

## Details

- **Entry Criteria**:
  - Price above SuperTrend and deviation > average + stddev × multiplier → buy.
  - Price below SuperTrend and deviation < -(average + stddev × multiplier) → sell.
- **Long/Short**: Both directions can be enabled.
- **Exit Criteria**:
  - Opposite deviation or SuperTrend reversal.
- **Stops**: No stop logic by default.
- **Default Values**:
  - MA length = 20.
  - StdDev length = 20.
  - StdDev multiplier = 1.0.
  - ATR period = 10.
  - ATR factor = 3.0.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, StandardDeviation, SuperTrend
  - Stops: None
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
