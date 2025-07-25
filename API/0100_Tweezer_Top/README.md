# Tweezer Top Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Tweezer Top mirrors the bottom version but appears after an advance.
Two candles share nearly the same high, showing that buyers could not push beyond a certain level.

The strategy opens a short once the second candle confirms the ceiling, expecting a pullback as bullish momentum stalls.

A tight stop above the twin highs keeps risk in check and the trade exits if price climbs back above that resistance.

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

Testing indicates an average annual return of about 187%. It performs best in the stocks market.
