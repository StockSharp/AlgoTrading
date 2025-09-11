# Smoothed Heiken-Ashi Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Heiken-Ashi candles smoothed with EMA highlight acceleration in price moves. A long position is opened when a bullish smoothed candle has a larger body than the previous one. The position is closed when a bearish body expands.

## Details

- **Entry Criteria**: bullish smoothed Heiken-Ashi candle with larger body than previous
- **Long/Short**: Long
- **Exit Criteria**: bearish body expands
- **Stops**: No
- **Default Values**:
  - `EmaLength` = 40
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: EMA, Heikin-Ashi
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
