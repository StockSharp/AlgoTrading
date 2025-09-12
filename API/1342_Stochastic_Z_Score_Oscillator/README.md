# Stochastic Z-Score Oscillator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Combines a rescaled Stochastic oscillator with a price Z-Score. A trade is opened when their average crosses a threshold and closed when the Z-Score returns to zero. Cooldown counters prevent frequent signals.

## Details

- **Entry Criteria**: average of rescaled Stochastic %K and price Z-Score crosses above/below threshold after cooldown
- **Long/Short**: Both
- **Exit Criteria**: Z-Score crossing zero
- **Stops**: No
- **Default Values**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `StochLength` = 14
  - `StochSmooth` = 7
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Stochastic, SMA, StandardDeviation
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
