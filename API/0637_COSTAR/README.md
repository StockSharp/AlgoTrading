# COSTAR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The COSTAR strategy builds a linear regression of closing prices and measures the standard deviation of residuals. Upper and lower bands are created by adding and subtracting the deviation multiplied by a factor. Trades attempt to fade extreme deviations and exit when price returns to the regression line.

## Details

- **Entry Criteria**:
  - **Long**: Price crosses above the lower band.
  - **Short**: Price crosses below the upper band.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Price crosses back through the regression line.
- **Stops**: None.
- **Default Values**:
  - `Length` = 100
  - `Multiplier` = 1
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Linear Regression, Standard Deviation
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
