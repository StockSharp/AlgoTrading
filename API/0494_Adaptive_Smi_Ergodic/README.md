# Adaptive SMI Ergodic Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Adaptive SMI Ergodic strategy uses the True Strength Index (TSI) oscillator with an EMA signal line to detect reversals from overbought or oversold extremes. A long position is opened when TSI crosses above the oversold threshold while staying above its signal line. A short position is opened when TSI crosses below the overbought threshold and is below the signal line.

## Details

- **Entry Criteria**:
  - TSI crosses above oversold and TSI > signal (long).
  - TSI crosses below overbought and TSI < signal (short).
- **Long/Short**: Both.
- **Exit Criteria**:
  - Reverse signal triggers opposite trade.
- **Stops**: None.
- **Default Values**:
  - `LongLength` = 12
  - `ShortLength` = 5
  - `SignalLength` = 5
  - `OversoldThreshold` = -0.4
  - `OverboughtThreshold` = 0.4
- **Filters**:
  - Category: Momentum oscillator
  - Direction: Long/Short
  - Indicators: True Strength Index, EMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
