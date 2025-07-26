# Bearish Harami Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Bearish Harami is the inverse of the bullish version, appearing after an upswing.
Here a small candle forms completely inside the prior bullish bar, hinting that upward momentum is stalling.

The strategy sells short when that inside candle closes, betting on a reversal as buyers lose conviction.

A percent stop above the pattern high caps the risk and the trade exits if price breaks to new highs.

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

Testing indicates an average annual return of about 43%. It performs best in the stocks market.
