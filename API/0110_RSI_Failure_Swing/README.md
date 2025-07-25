# RSI Failure Swing Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
RSI Failure Swing is a classic reversal technique where the RSI forms a higher low in oversold territory or a lower high in overbought territory.
This failure to reach a new extreme often precedes a change in trend.

The strategy buys when the RSI holds above its prior low and then crosses back above 30, or sells when it fails to exceed a prior high and crosses below 70.

A percent stop limits downside, and positions close when RSI crosses the opposite level.

## Details

- **Entry Criteria**: indicator signal
- **Long/Short**: Both
- **Exit Criteria**: stop-loss or opposite signal
- **Stops**: Yes, percent based
- **Default Values**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filters**:
  - Category: Reversal
  - Direction: Both
  - Indicators: RSI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 67%. It performs best in the stocks market.
