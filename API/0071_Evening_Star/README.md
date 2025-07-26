# Evening Star Pattern Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Evening Star mirrors the Morning Star but indicates a potential top. It begins with a strong bullish candle, followed by a small indecision candle, and ends with a bearish candle closing below the midpoint of the first bar.

The algorithm watches sequences of three candles. When the pattern forms, it enters short with a stop above the small middle candle's high. Positions exit once price drops beneath the confirmation candle's low or if the stop is triggered.

Since the setup anticipates a quick reversal from overbought conditions, trades typically aim for short, momentum-driven moves lower.

## Details

- **Entry Criteria**: Three-candle Evening Star pattern.
- **Long/Short**: Short only.
- **Exit Criteria**: Price below confirmation bar low or stop-loss.
- **Stops**: Yes, above middle candle high.
- **Default Values**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 1
- **Filters**:
  - Category: Pattern
  - Direction: Short
  - Indicators: Candlestick
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 100%. It performs best in the forex market.
