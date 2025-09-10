# Arpeet MACD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Arpeet MACD strategy trades MACD crossovers with a zero-line filter. A long signal appears when the MACD line crosses above the signal line while remaining below zero. A short signal occurs when the MACD crosses below the signal line above zero.

## Details

- **Entry Criteria**:
  - **Long**: MACD crosses above signal and MACD < 0.
  - **Short**: MACD crosses below signal and MACD > 0.
- **Stops**: None.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
- **Filters**:
  - Category: Indicator
  - Direction: Both
  - Indicators: MACD
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
