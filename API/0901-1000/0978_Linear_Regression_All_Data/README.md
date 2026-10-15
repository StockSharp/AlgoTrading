# Linear Regression All Data Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy calculates a linear regression line using all available bars and plots it on the chart.
It also logs slope, intercept and correlation coefficients.

## Details

- **Entry Criteria**: None.
- **Long/Short**: None.
- **Exit Criteria**: None.
- **Stops**: No.
- **Default Values**:
  - `MaxBarsBack` = 5000.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filters**:
  - Category: Utility
  - Direction: None
  - Indicators: Linear Regression
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
