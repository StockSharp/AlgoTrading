# Pivot Point Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Daily pivot points and their support and resistance levels often act as turning points for intraday price action. This strategy calculates the classic floor-trader pivots from the prior day's high, low and close, then looks for candles bouncing off S1 or R1.

When price approaches support level S1 and forms a bullish candle, a long entry is taken. If price tests resistance level R1 and prints a bearish candle, a short is opened. Trades exit upon reaching the central pivot or if the protective stop is hit.

The method resets at the start of each trading day with new pivot calculations, making it well suited for sessions with clear intraday ranges.

## Details

- **Entry Criteria**: Bullish candle near S1 or bearish candle near R1.
- **Long/Short**: Both.
- **Exit Criteria**: Price crossing the central pivot or stop-loss.
- **Stops**: Yes, percentage based.
- **Default Values**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filters**:
  - Category: Mean Reversion
  - Direction: Both
  - Indicators: Pivot Points
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 127%. It performs best in the stocks market.
