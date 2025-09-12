# Ehlers SwamiCharts RSI
[Русский](README_ru.md) | [中文](README_cn.md)

Averages RSI values from periods 2–48 to build a color map. Long when average color is green, short when red.

## Details

- **Entry Criteria**: Average color green (`Color1Avg` == 255 and `Color2Avg` > `LongColor`) for long; red (`Color1Avg` > `ShortColor` and `Color2Avg` == 255) for short.
- **Long/Short**: Both.
- **Exit Criteria**: Opposite signal.
- **Stops**: No.
- **Default Values**:
  - `LongColor` = 50
  - `ShortColor` = 50
  - `CandleType` = 5 minutes
- **Filters**:
  - Category: Oscillator
  - Direction: Both
  - Indicators: RSI
  - Stops: No
  - Complexity: Advanced
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
