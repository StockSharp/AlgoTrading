# Fibonacci-Only Strategy V2
[Русский](README_ru.md) | [中文](README_cn.md)

Trades around 19% and 82.56% Fibonacci retracements calculated over 93 candles. Entries occur when price touches or breaks these levels with candle confirmation. Risk is managed via optional ATR-based stop loss and trailing stop.

## Details

- **Entry Criteria**: touch or breakout of 19% / 82.56% Fibonacci levels with candle confirmation
- **Long/Short**: Both
- **Exit Criteria**: stop loss or trailing stop
- **Stops**: Yes
- **Default Values**:
  - `CandleType` = 15 minutes
- **Filters**:
  - Category: Fibonacci breakout
  - Direction: Both
  - Indicators: ATR, Highest, Lowest
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
