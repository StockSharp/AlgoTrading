# Volatility Bias Model
[Русский](README_ru.md) | [中文](README_cn.md)

Counts bullish vs bearish closes over a window and trades in the direction of the dominant bias when volatility is sufficient. Uses ATR targets and exits after a maximum number of bars.

## Details
- **Entry Criteria**: Bias ratio above `BiasThreshold` for long or below `1 - BiasThreshold` for short with range above `RangeMin`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop, take profit, or `MaxBars` reached.
- **Stops**: Yes.
- **Default Values**:
  - `BiasWindow` = 10
  - `BiasThreshold` = 0.6
  - `RangeMin` = 0.05
  - `RiskReward` = 2
  - `MaxBars` = 20
  - `AtrLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: ATR, SMA, Highest, Lowest
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
