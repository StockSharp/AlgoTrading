# CCI Hook Reversal Strategy

The CCI Hook Reversal uses the Commodity Channel Index as a trigger when it hooks away from an extreme reading.
After the indicator pushes above +100 or below -100 it often snaps back quickly as momentum stalls.

Long trades occur when CCI turns up from oversold while price still prints a marginal new low.
Shorts are initiated when CCI rolls over from overbought with price poking to new highs.

Each trade carries a small fixed stop and is exited when the CCI hooks back in the opposite direction or the stop is reached.

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
  - Indicators: CCI
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
