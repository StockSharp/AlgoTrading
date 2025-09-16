# MACD Signal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the difference between the MACD line and its signal line.
A position is opened when the difference crosses an ATR-based threshold and is closed on opposite crossings.
A trailing stop and fixed take-profit are applied in ticks.

## Details

- **Entry Criteria**:
  - **Long**: MACD - Signal crosses above `ATR * Level`.
  - **Short**: MACD - Signal crosses below `-ATR * Level`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Opposite threshold crossing.
- **Stops**:
  - Fixed take-profit in ticks.
  - Optional trailing stop.
- **Indicators**:
  - MACD (configurable fast, slow, signal periods).
  - ATR(200) to scale threshold.
