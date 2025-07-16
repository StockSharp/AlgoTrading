# Bullish Harami Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Bullish Harami is a two-candle pattern where a small body is contained within the range of the prior bearish candle.
It hints that selling momentum has stalled and buyers may step back in.

This strategy enters long once the second candle closes inside the first, expecting follow-through to the upside on the next bar.

A percent stop beneath the pattern provides protection, and the trade exits if price slips back below the setup.

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
