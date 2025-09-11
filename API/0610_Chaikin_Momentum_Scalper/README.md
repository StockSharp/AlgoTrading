# Chaikin Momentum Scalper
[Русский](README_ru.md) | [中文](README_cn.md)

This scalping strategy uses the Chaikin oscillator to capture momentum shifts. Long trades occur when the oscillator crosses above zero and price is above the 200-period SMA. Short trades occur on a cross below zero with price below the SMA. ATR multiples define stop-loss and take-profit levels.

## Details

- **Entry Criteria**: Chaikin oscillator crosses above/below zero with price above/below SMA.
- **Long/Short**: Both.
- **Exit Criteria**: ATR-based stop-loss and take-profit.
- **Stops**: Yes.
- **Default Values**:
  - `FastLength` = 3
  - `SlowLength` = 10
  - `SmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplierSL` = 1.5m
  - `AtrMultiplierTP` = 2.0m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Chaikin Oscillator, SMA, ATR
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
