# Long EMA Advanced Exit
[Русский](README_ru.md) | [中文](README_cn.md)

Long EMA Advanced Exit is a long-only strategy that enters when a short moving average crosses above a medium one and price is above a long moving average. Exits can be triggered by MACD cross down, price closing below a selected moving average, MA cross down, trailing stop, or an ATR-based volatility filter.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Short MA crosses above medium MA and price is above long MA.
- **Exit Criteria**: MACD cross down, price below selected MA, short MA crossing below medium MA, optional trailing stop.
- **Stops**: Optional trailing stop.
- **Default Values**:
  - `MaType` = EMA
  - `EntryConditionType` = Crossover
  - `LongTermPeriod` = 200
  - `ShortTermPeriod` = 5
  - `MidTermPeriod` = 10
  - `EnableMacdExit` = true
  - `MacdCandleType` = TimeSpan.FromDays(7).TimeFrame()
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 15
  - `UseMaCloseExit` = false
  - `MaCloseExitPeriod` = 50
  - `UseMaCrossExit` = true
  - `UseVolatilityFilter` = false
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filters**:
  - Category: Trend following
  - Direction: Long only
  - Indicators: MA, MACD, ATR
  - Complexity: Medium
  - Risk level: Medium
