# Ichimoku RSI Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Ichimoku RSI uses Ichimoku cloud levels to define trend direction while RSI pinpoints short-term pullbacks.
Trades align with the cloud, entering when RSI recovers from oversold in an uptrend or falls from overbought in a downtrend.

By combining a broad trend filter with a momentum oscillator, the strategy aims to join strong moves after brief pauses.

Stops are placed beyond the cloud boundary to protect against deeper corrections.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Ichimoku, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 142%. It performs best in the stocks market.
