# Pearson's R Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Pearson's R Oscillator strategy dynamically searches for the period where price best fits a linear regression channel using the Pearson correlation coefficient. When the correlation reaches the specified positive or negative threshold, the strategy forms a regression channel and trades breakouts.

Positions are opened when price crosses the channel boundaries and can be closed on midline crosses. The approach adapts to market conditions by automatically adjusting the lookback window to the strongest correlation.

## Details

- **Entry Criteria**:
  - Price crosses above the upper regression line → **Long**.
  - Price crosses below the lower regression line → **Short**.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Midline cross in the opposite direction.
- **Stops**: None.
- **Default Values**:
  - `MinPeriod` = 48
  - `MaxPeriod` = 360
  - `Step` = 12
  - `IdealPositive` = 0.85
  - `IdealNegative` = -0.85
  - `Deviations` = 2
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Pearson's R, Linear Regression
  - Stops: None
  - Complexity: Medium
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
