# Equilibrium Candles Pattern Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy using equilibrium candles to detect short trends and enter on pullbacks. The equilibrium is the midpoint between the highest and lowest prices over a lookback window. After a bullish or bearish streak, a move back through the equilibrium triggers an entry. ATR is used for optional stop/target and to exit on unusually large candles.

## Details

- **Entry Criteria**:
  - **Long**: After bullish trend when price crosses below equilibrium.
  - **Short**: After bearish trend when price crosses above equilibrium.
- **Long/Short**: Both
- **Stops**: ATR-based stop loss and take profit (optional)
- **Default Values**:
  - `EquilibriumLength` = 9
  - `CandlesForTrend` = 7
  - `MaxPullbackCandles` = 2
  - `AtrPeriod` = 14
  - `StopMultiplier` = 2
  - `UseTpSl` = true
  - `UseBigCandleExit` = true
  - `BigCandleMultiplier` = 1
  - `UseReverse` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
