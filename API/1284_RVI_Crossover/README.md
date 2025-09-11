# RVI Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The RVI Crossover strategy uses the Relative Vigor Index and a moving average filter.
It buys when the RVI crosses above its signal line while price is below the EMA, and sells when the RVI crosses below the signal while price is above the EMA.

## Details

- **Entry Criteria**: RVI crossing its signal line with EMA vs VWMA filter
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `RviLength` = 10
  - `SignalLength` = 10
  - `EmaLength` = 31
  - `VwmaLength` = 1
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: RVI, SMA, EMA, VWMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
