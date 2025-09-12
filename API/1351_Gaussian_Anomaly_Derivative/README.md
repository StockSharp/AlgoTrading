# Gaussian Anomaly Derivative Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Uses a moving average of price anomaly `1 - (high + low) / (2 * close)` and its smoothed derivative.
Trades long when the derivative exceeds its positive threshold and short when it drops below the negative threshold.

## Details

- **Entry Criteria**: anomaly or its derivative crosses threshold
- **Long/Short**: Configurable
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `UseSma` = true
  - `MaPeriod` = 3
  - `DerivativeMaPeriod` = 2
  - `ThresholdCoeff` = 1.0
  - `DerivativeThresholdCoeff` = 1.0
  - `StartBarCount` = 600
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
