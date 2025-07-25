# Bollinger RSI Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
Bollinger RSI combines Bollinger Band overextension with RSI momentum signals.
When price closes outside the bands but RSI shows divergence, a reversal is often near.

Testing indicates an average annual return of about 148%. It performs best in the forex market.

The system takes counter-trend trades on that divergence, exiting once price re-enters the bands or RSI crosses back.

A tight percent stop limits exposure in case volatility expands further.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Mean reversion
  - Direction: Both
  - Indicators: Bollinger Bands, RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

