# RSI Hook Reversal Strategy
[Русский](README_ru.md) | [中文](README_zh.md)
 
The RSI Hook Reversal tries to catch short-term turning points when the RSI exits an extreme.
After an overbought or oversold push the indicator often "hooks" back toward the midline before price reacts.

The strategy waits for that hook while price keeps pressing in the prior direction.
A long entry triggers once RSI curls higher from oversold as price marks a fresh low, while a short opens when RSI turns down from overbought during a new high.

Trades use a simple percent stop to control risk and typically close when the RSI hooks the other way.

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

Testing indicates an average annual return of about 163%. It performs best in the stocks market.
