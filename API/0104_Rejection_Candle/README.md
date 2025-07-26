# Rejection Candle Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
A Rejection Candle forms when price probes a level but fails to hold beyond it, leaving a long wick and small body.
Such candles indicate an attempt to move in one direction was firmly rejected by the market.

Testing indicates an average annual return of about 49%. It performs best in the crypto market.

The strategy enters in the opposite direction of the wick once the candle closes, expecting price to reverse back through the range.

Stops are set outside the rejected high or low to cap risk, and trades exit if momentum fails to materialize.

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

