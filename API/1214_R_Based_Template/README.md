# R Based Strategy Template
[Русский](README_ru.md) | [中文](README_cn.md)

RSI-based strategy with risk-managed position sizing and configurable stop types.

## Details

- **Entry Criteria**:
  - Long when RSI crosses below `OversoldLevel`.
  - Short when RSI crosses above `OverboughtLevel`.
- **Long/Short**: Both.
- **Exit Criteria**: Stop loss or take profit using `TpRValue` multiple.
- **Stops**:
  - Fixed, Atr, Percentage or Ticks.
- **Default Values**:
  - `RiskPerTradePercent` = 1
  - `RsiLength` = 14
  - `OversoldLevel` = 30
  - `OverboughtLevel` = 70
  - `StopLossType` = Fixed
  - `SlValue` = 100
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `TpRValue` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI, ATR
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Variable
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
