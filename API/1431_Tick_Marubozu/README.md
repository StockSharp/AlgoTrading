# Tick Marubozu Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Identifies Marubozu candles on tick data and confirms them with high volume. Buys bullish Marubozu and sells bearish ones.

## Details

- **Entry Criteria**: bullish or bearish Marubozu with volume above SMA
- **Long/Short**: Both
- **Exit Criteria**: Opposite signal
- **Stops**: No
- **Default Values**:
  - `TickSize` = 5
  - `VolLength` = 20
  - `CandleType` = 1-minute time frame
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: SMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
