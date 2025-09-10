# Bollinger Band Touch with SMI and MACD Angle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy buys when price touches the lower Bollinger Band and both SMI and MACD angles point upward. The position is closed once price reaches the upper Bollinger Band.

## Details

- **Entry Criteria**:
  - **Long**: Close price touches or falls below the lower Bollinger Band and SMI/MACD angles are positive but below their thresholds.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - **Long**: Close price touches or exceeds the upper Bollinger Band.
- **Stops**: None.
- **Default Values**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2.0
  - `SmiLength` = 14
  - `SmiSignalLength` = 3
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `SmiAngleThreshold` = 60
  - `MacdAngleThreshold` = 50
- **Filters**:
  - Category: Mean reversion
  - Direction: Long
  - Indicators: Bollinger Bands, Stochastic (SMI), MACD
  - Stops: None
  - Complexity: Low
  - Timeframe: 1H
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
