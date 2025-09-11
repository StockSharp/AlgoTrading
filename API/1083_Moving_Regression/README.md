# Moving Regression
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that applies polynomial moving regression to predict the next price. A long position opens when the forecast is above the current value and a short position when below.

## Details

- **Entry Criteria**: Forecast direction.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `Degree` = 2
  - `Window` = 18
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Polynomial Regression
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday (5m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
