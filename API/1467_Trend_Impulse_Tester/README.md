# Trend Impulse Tester Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trend Impulse Tester enters trades when a strong trend is confirmed by EMAs and ADX and an RSI impulse appears.
It buys on bullish impulses during uptrends and sells on bearish impulses during downtrends.

## Details

- **Entry Criteria**: EMA trend + ADX confirmation with RSI crossing threshold
- **Long/Short**: Both
- **Exit Criteria**: opposite signal
- **Stops**: No
- **Default Values**:
  - `FastEmaLength` = 50
  - `SlowEmaLength` = 200
  - `AdxLength` = 14
  - `AdxMin` = 18
  - `RsiLength` = 14
  - `RsiUp` = 55
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, ADX, RSI
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
