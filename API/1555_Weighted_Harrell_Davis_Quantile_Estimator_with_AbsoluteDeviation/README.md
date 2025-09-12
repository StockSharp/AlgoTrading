# Weighted Harrell-Davis Quantile Estimator with AbsoluteDeviation Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses a median-based quantile estimator with absolute deviation bands to detect price outliers.
It buys when the close drops below the lower band and sells when the close rises above the upper band.

## Details

- **Entry Criteria**: close below lower deviation band or above upper band
- **Long/Short**: Both
- **Exit Criteria**: opposite band cross
- **Stops**: No
- **Default Values**:
  - `Length` = 39
  - `DeviationMultiplier` = 1.213
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Median
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

