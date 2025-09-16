# FatlSatlOsma Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This example reproduces logic of the MetaTrader expert **Exp_FatlSatlOsma** using StockSharp high level API.  
The original system works with the Fatl/Satl oscillator (a custom indicator similar to MACD).  
The strategy looks for a change in oscillator direction:

- When the oscillator rises for two bars and the last value is higher than the previous, a long position is opened and short positions are closed.
- When the oscillator falls for two bars and the last value is lower than the previous, a short position is opened and long positions are closed.

The oscillator is implemented through the built-in `MovingAverageConvergenceDivergenceSignal` indicator with configurable fast and slow periods.  
Default values correspond to the original FATL/SATL parameters.

## Details

- **Entry Criteria**: oscillator acceleration.
- **Long/Short**: both.
- **Exit Criteria**: opposite acceleration.
- **Stops**: none.
- **Default Values**:
  - `Fast` = 39
  - `Slow` = 65
  - `CandleType` = 12 hour time frame
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: MACD
  - Stops: No
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural networks: No
  - Divergence: Yes
  - Risk level: Medium
