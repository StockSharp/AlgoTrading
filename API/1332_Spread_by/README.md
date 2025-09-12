# Spread By Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Spread By uses a moving average with standard deviation bands to trade price extremes.
It buys when price falls below the lower band and sells when price rises above the upper band.

## Details

- **Entry Criteria**: price moves beyond ±1 standard deviation from the moving average
- **Long/Short**: Both
- **Exit Criteria**: price returns to the moving average
- **Stops**: No
- **Default Values**:
  - `Length` = 100
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: SMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
