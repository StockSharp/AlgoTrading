# Xbug Free Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Contrarian moving average strategy that buys when price crosses below its moving average and sells when price crosses above. Uses symmetric take-profit and stop-loss distances.

## Details

- **Entry Criteria**: price crossing below/above simple moving average
- **Long/Short**: Both
- **Exit Criteria**: opposite signal or protective stop
- **Stops**: Yes
- **Default Values**:
  - `MaPeriod` = 19
  - `MaShift` = 15
  - `StopPoints` = 270
  - `Volume` = 0.1
  - `CandleType` = 4-hour
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: MA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Swing
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
