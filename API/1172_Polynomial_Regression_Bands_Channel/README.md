# Polynomial Regression Bands Channel Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy fits a polynomial regression line to recent prices and builds upper and lower bands from the standard deviation of residuals. Long positions are opened when price falls below the lower band and short positions are opened when price rises above the upper band.

## Details

- **Entry Criteria**:
  - **Long**: `Close < LowerBand`.
  - **Short**: `Close > UpperBand`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Length` = 100.
  - `Degree` = 2.
  - `Std Dev Multiplier` = 2.
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Polynomial regression
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
