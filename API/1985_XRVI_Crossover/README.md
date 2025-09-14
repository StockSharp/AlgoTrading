# XRVI Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The XRVI Crossover strategy is based on the Extended Relative Vigor Index (XRVI).
XRVI is calculated by smoothing the Relative Vigor Index and then applying a second moving average to produce a signal line.
The strategy enters long when XRVI crosses above the signal line and enters short when it crosses below.
Existing positions are reversed on opposite signals.

## Details

- **Entry Criteria**: XRVI crossing its signal line
- **Long/Short**: Both
- **Exit Criteria**: Opposite crossover
- **Stops**: No
- **Default Values**:
  - `RviLength` = 10
  - `SignalLength` = 5
  - `CandleType` = H4 timeframe
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: Relative Vigor Index, Simple Moving Average
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
