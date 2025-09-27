# Intra Bullish Strategy - Profit Ping v4.0 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Long-only system using an EMA crossover confirmed by MACD histogram and RSI strength.

## Details

- **Entry Criteria**:
  - Short EMA crosses above Long EMA
  - MACD histogram > 0
  - RSI > 50
  - Close > Open
- **Exit Criteria**:
  - Short EMA crosses below Long EMA
  - MACD histogram < 0
  - RSI < 50
  - Close < Open
- **Indicators**:
  - Exponential Moving Averages
  - MACD
  - RSI
- **Stops**: None.
- **Default Values**:
  - `ShortEmaLength` = 7
  - `LongEmaLength` = 14
  - `RsiLength` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
- **Filters**:
  - Trend-following
  - Single timeframe
  - Indicators: EMA, MACD, RSI
  - Stops: none
  - Complexity: Low
