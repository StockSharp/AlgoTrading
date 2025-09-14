# Puria Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Puria is a trend-following strategy that combines a fast EMA, two slow LWMAs of the low price, and a MACD filter. A long position is opened when the 5-period EMA is above both the 75- and 85-period LWMAs, the previous close is above the EMA, and the MACD line is positive. A short position is opened when the opposite conditions are met. The strategy uses fixed take-profit and stop-loss levels and allows only one position per direction until an opposite signal appears.

## Details
- **Entry Criteria**: EMA(5) above LWMA(75) and LWMA(85), previous close above EMA, MACD(15,26) > 0 for longs; reverse for shorts.
- **Long/Short**: Both directions.
- **Exit Criteria**: Stop-loss or take-profit.
- **Stops**: Fixed stop-loss and take-profit distances in price points.
- **Default Values**:
  - `StopLoss` = 14
  - `TakeProfit` = 15
  - `Ma1Period` = 75
  - `Ma2Period` = 85
  - `Ma3Period` = 5
  - `CandleType` = 1-minute timeframe
- **Filters**: MACD zero-line filter.
