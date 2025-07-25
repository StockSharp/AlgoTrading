# False Breakout Trap Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The False Breakout Trap aims to capitalize on breaks that fail to hold beyond key support or resistance.
Traders often jump into a breakout only to see price quickly reverse, leaving them trapped.

This strategy waits for that failure, entering in the opposite direction once price closes back inside the range.

Stop placement is tight, just beyond the failed breakout level, ensuring losses stay small if the reversal doesn't materialize.

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
  - Indicators: Price Action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

Testing indicates an average annual return of about 52%. It performs best in the crypto market.
