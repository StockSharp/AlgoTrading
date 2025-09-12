# VAWSI and Trend Persistence Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Reversal strategy combining VAWSI, trend persistence and ATR to build a dynamic threshold on Heikin-Ashi candles.

## Details

- **Entry Criteria**: Heikin-Ashi close crosses above/below dynamic threshold
- **Long/Short**: Both
- **Exit Criteria**: Opposite crossover or protective stops
- **Stops**: Yes, percentage based
- **Default Values**:
  - `CandleType` = 15 minute
  - `SlTp` = 5
  - `RsiWeight` = 100
  - `TrendWeight` = 79
  - `AtrWeight` = 20
  - `CombinationMult` = 1
  - `Smoothing` = 3
  - `CycleLength` = 20
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: RSI, ATR
  - Stops: Yes
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
