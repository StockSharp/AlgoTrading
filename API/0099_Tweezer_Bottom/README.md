# Tweezer Bottom Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Tweezer Bottom is a two-candle reversal pattern that appears after a decline.
Both candles share a similar low, signaling that sellers failed to push beyond that level.

This strategy goes long after the second candle confirms the shared bottom, anticipating a bounce as selling pressure dries up.

Stops are placed just beneath the common low to manage risk, and the position exits if price fails to rally.

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
