# Dynamic Volatility Differential Model
[Русский](README_ru.md) | [中文](README_zh.md)

The **Dynamic Volatility Differential Model (DVDM)** strategy compares implied volatility with historical volatility. It opens long when implied volatility exceeds realized volatility by a dynamic standard deviation threshold and enters short when the spread falls below the negative threshold.

Signals use daily data and do not rely on stops.

## Details
- **Entry Criteria**: Volatility spread above/below dynamic standard deviation thresholds.
- **Long/Short**: Both directions.
- **Exit Criteria**: Volatility spread crossing the zero line.
- **Stops**: No.
- **Default Values**:
  - `Length = 5`
  - `StdevMultiplier = 7.1m`
  - `VolatilitySecurity = "TVC:VIX"`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Volatility
  - Direction: Both
  - Indicators: StandardDeviation
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
