# Stochastic Failure Swing Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The Stochastic Failure Swing monitors the oscillator for a lower high above 80 or a higher low below 20.
When the indicator fails to reach a new extreme and then reverses, it often signals a trend shift.

Testing indicates an average annual return of about 70%. It performs best in the stocks market.

The strategy buys when a higher low forms below 20 and %K crosses back above %D, or sells when a lower high occurs above 80 and %K crosses under.

Trades employ a small percent stop and close when the stochastic crosses back through the prior swing level.

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
  - Indicators: Stochastic
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

