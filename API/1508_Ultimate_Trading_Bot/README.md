# Ultimate Trading Bot
[Русский](README_ru.md) | [中文](README_cn.md)

Long-only strategy combining RSI, moving average, MACD and Stochastic crossovers to time entries and exits.

## Details

- **Entry Criteria**: RSI crossing above oversold while price above MA, MACD and Stochastic cross up.
- **Long/Short**: Long only.
- **Exit Criteria**: Opposite cross conditions.
- **Stops**: No explicit stops.
- **Default Values**:
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MaLength` = 50
  - `StochLength` = 14
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: RSI, MA, MACD, Stochastic
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
