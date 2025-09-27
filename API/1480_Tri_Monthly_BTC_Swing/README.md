# Tri-Monthly BTC Swing Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Tri-Monthly BTC Swing trades with EMA200, MACD crossover, and RSI filter.
The strategy allows only one trade every 90 days.

## Details

- **Entry Criteria**: close above EMA200, MACD line above signal, RSI above threshold, and at least 90 days since last trade
- **Long/Short**: Long
- **Exit Criteria**: MACD line below signal or RSI below threshold
- **Stops**: No
- **Default Values**:
  - `EmaLength` = 200
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiThreshold` = 50
  - `TradeInterval` = 90 days
  - `CandleType` = 1 day
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA, MACD, RSI
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
