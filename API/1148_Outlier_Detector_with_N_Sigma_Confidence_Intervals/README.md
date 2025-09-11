# Outlier Detector with N-Sigma Confidence Intervals Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy identifies outliers in price changes using N-sigma confidence intervals and trades mean reversion when extreme moves occur.

## Details

- **Entry Criteria**:
  - Short when z-score > `SecondLimit`.
  - Long when z-score < -`SecondLimit`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Close position when |z-score| < `FirstLimit`.
- **Stops**: None.
- **Default Values**:
  - `SampleSize` = 30
  - `FirstLimit` = 2
  - `SecondLimit` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: StandardDeviation, Z-Score
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Risk level: Medium
