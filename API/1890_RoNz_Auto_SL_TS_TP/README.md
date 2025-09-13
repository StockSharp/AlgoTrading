# RoNz Auto SL TS TP Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy that opens positions on EMA crossover and automatically manages stop-loss and take-profit levels.  
After entry it sets initial stop and target, then optionally locks profit and activates a trailing stop.

## Details

- **Entry Criteria**:
  - Long: `EMA10 < EMA20 && EMA10 > EMA100`
  - Short: `EMA10 > EMA20 && EMA10 < EMA100`
- **Long/Short**: Both
- **Exit Criteria**: Stop loss, take profit, profit lock or trailing stop
- **Stops**: Yes
- **Default Values**:
  - `TakeProfit` = 500
  - `StopLoss` = 250
  - `LockProfitAfter` = 100
  - `ProfitLock` = 60
  - `TrailingStop` = 50
  - `TrailingStep` = 10
- **Filters**:
  - Category: Risk management
  - Direction: Both
  - Indicators: EMA
  - Stops: SL/TP/Trailing
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
