# Directed Movement Candle Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy monitors the Relative Strength Index (RSI) on candle closes. When RSI leaves the neutral zone and crosses user-defined levels, the strategy opens positions in the direction of momentum and closes any opposite exposure.

## Details

- **Indicator**: Relative Strength Index with adjustable `RsiPeriod`.
- **HighLevel**: RSI value indicating bullish momentum.
- **MiddleLevel**: Neutral threshold kept for reference.
- **LowLevel**: RSI value indicating bearish momentum.
- **Entry**:
  - Long when RSI rises above `HighLevel` after being below it.
  - Short when RSI falls below `LowLevel` after being above it.
- **Exit**: Opposite signal closes existing position before a new one is opened.
- **Long/Short**: Both directions.
- **Stops**: Not used by default.
- **Default Values**:
  - `RsiPeriod` = 14
  - `HighLevel` = 70
  - `MiddleLevel` = 50
  - `LowLevel` = 30
  - `CandleType` = 5-minute timeframe
