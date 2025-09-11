# Linear Correlation Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Linear Correlation Oscillator strategy measures the correlation between price and time over a rolling window. The strategy goes long when the oscillator crosses above zero and goes short when it crosses below zero.

## Details

- **Entry Criteria**:
  - Oscillator crosses above zero → **Long**.
  - Oscillator crosses below zero → **Short**.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Opposite zero cross.
- **Stops**: None.
- **Default Values**:
  - `Length` = 14
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Linear Correlation
  - Stops: None
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
