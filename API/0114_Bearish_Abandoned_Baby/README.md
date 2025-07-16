# Bearish Abandoned Baby Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Bearish Abandoned Baby mirrors the bullish version but signals a potential top.
It features a gap up doji followed by a gap down, leaving the middle candle isolated above the prior range.

The strategy sells short when the third candle gaps below the doji, aiming to profit from the abrupt shift in sentiment.

Risk is limited with a stop just above the doji high in case price recovers.

## Details

- **Entry Criteria**: pattern match
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Pattern
  - Direction: Both
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
