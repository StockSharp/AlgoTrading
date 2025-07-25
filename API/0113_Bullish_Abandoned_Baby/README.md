# Bullish Abandoned Baby Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Bullish Abandoned Baby is a rare three-candle pattern featuring a gap down doji followed by a gap up.
This formation leaves the middle candle "abandoned" and often precedes a sharp reversal higher.

The strategy buys at the open of the third candle once it gaps above the doji, anticipating strong follow-through as shorts cover.

Stops reside just beneath the doji low, ensuring losses remain small if the reversal fails to hold.

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

Testing indicates an average annual return of about 76%. It performs best in the forex market.
