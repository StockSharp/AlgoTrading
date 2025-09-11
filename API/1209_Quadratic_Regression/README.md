# Quadratic Regression Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy calculates a quadratic regression line for the last `Length` bars and trades on price crossovers with the regression line.

## Details

- **Entry Criteria**: Price crosses above/below quadratic regression line.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `Length` = 54.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Quadratic Regression
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
