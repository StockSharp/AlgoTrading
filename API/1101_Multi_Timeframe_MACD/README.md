# Multi-Timeframe MACD
[Русский](README_ru.md) | [中文](README_cn.md)

Multi-Timeframe MACD combines MACD signals from the working timeframe and a higher timeframe. Entries occur when both timeframes agree using line crossovers or zero-line crossings.

## Details
- **Data**: Price candles from two timeframes.
- **Entry Criteria**:
  - **Long**: Depends on `Entry` parameter. By default, bullish crossover on both timeframes.
  - **Short**: Opposite of long.
- **Exit Criteria**: Opposite signal or trailing stop.
- **Stops**: Optional trailing stop.
- **Default Values**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = tf(5)
  - `HigherCandleType` = tf(1d)
  - `ShowCurrentTimeframe` = true
  - `ShowHigherTimeframe` = true
  - `Entry` = Crossover
  - `UseTrailingStop` = false
  - `TrailingStopPercent` = 2
- **Filters**:
  - Category: Trend
  - Direction: Long & Short
  - Indicators: MACD
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Multi-timeframe (5m/1d)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
