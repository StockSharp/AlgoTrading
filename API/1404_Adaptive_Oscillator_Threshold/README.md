# Adaptive Oscillator Threshold Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Adaptive Oscillator Threshold uses RSI with a dynamic threshold based on Bufi's Adaptive Threshold (BAT). It buys when the RSI falls below either a fixed level or an adaptive threshold.

## Details

- **Entry Criteria**: RSI drops below fixed or adaptive threshold
- **Long/Short**: Long
- **Exit Criteria**: Fixed-bar exit or dollar stop-loss
- **Stops**: Dollar stop-loss
- **Default Values**:
  - `UseAdaptiveThreshold` = true
  - `RsiLength` = 2
  - `BuyLevel` = 14
  - `AdaptiveLength` = 8
  - `AdaptiveCoefficient` = 6
  - `ExitBars` = 28
  - `DollarStopLoss` = 1600
- **Filters**:
  - Category: Oscillator
  - Direction: Long
  - Indicators: RSI, StandardDeviation, LinearRegression
  - Stops: Dollar
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
