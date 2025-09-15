# Spectral RVI Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Spectral RVI Crossover strategy smooths the Relative Vigor Index and its signal line and trades on their crossovers.
It buys when the smoothed RVI crosses above the smoothed signal line and sells when the opposite occurs.

## Details

- **Entry Criteria**: smoothed RVI crossing its smoothed signal line
- **Long/Short**: Both
- **Exit Criteria**: opposite crossover
- **Stops**: No
- **Default Values**:
  - `RviLength` = 14
  - `SignalLength` = 4
  - `SmoothLength` = 20
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RVI, SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: 4-hour
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
