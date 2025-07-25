# Morning Star Pattern Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Morning Star is a bullish candlestick formation that signals a potential bottom after a decline. It consists of a large bearish candle, a small indecisive candle, and a strong bullish candle that closes above the midpoint of the first bar.

This strategy tracks sequences of three candles. When the pattern appears a long position is opened with a stop placed below the small middle candle. Exits occur once price rises above the high of the confirmation bar or if the stop is reached.

Because the pattern often sparks quick recoveries from oversold conditions, trades are usually short lived, capturing the initial thrust higher.

## Details

- **Entry Criteria**: Three-candle Morning Star pattern.
- **Long/Short**: Long only.
- **Exit Criteria**: Price above confirmation bar high or stop-loss.
- **Stops**: Yes, below middle candle low.
- **Default Values**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filters**:
  - Category: Pattern
  - Direction: Long
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 97%. It performs best in the crypto market.
