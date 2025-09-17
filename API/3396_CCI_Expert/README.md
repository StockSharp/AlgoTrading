# CCI Expert Strategy

## Overview

This strategy is a StockSharp conversion of the original MetaTrader "CCI-Expert" robot. It uses the Commodity Channel Index (CCI) indicator on a single time frame and keeps the logic strictly sequential: the strategy waits for three completed candles before deciding to open or close a position.

## Trading Logic

1. Subscribe to the configured candle series and calculate a CCI with the chosen period.
2. Evaluate the latest three finished CCI values:
   - **Long setup**: the current and previous CCI values are above `+1`, while the second previous value was below `+1`.
   - **Short setup**: the current and previous CCI values are below `+1`, while the second previous value was above `+1`.
3. Open only one market position at a time when no position is active and the spread filter allows trading.
4. Close an existing position only if the opposite signal appears **and** the trade is already profitable (close price is better than the entry price).

## Risk Management

- The strategy can use either a fixed lot or calculate the volume from the risk percentage and the configured stop-loss distance.
- `StartProtection` automatically places stop-loss and take-profit brackets in price points.
- An optional spread filter blocks trading until the current bid/ask difference is below the `MaxSpreadPoints` threshold.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `FixedVolume` | Fixed order size. Set to zero to activate risk-based sizing. | 0.1 |
| `RiskPercent` | Percentage of current portfolio value used to size orders when `FixedVolume` is zero. | 0 |
| `TakeProfitPoints` | Take-profit distance measured in price points. | 150 |
| `StopLossPoints` | Stop-loss distance measured in price points. | 600 |
| `MaxSpreadPoints` | Maximum allowed spread (in price points). Zero disables the filter. | 30 |
| `CciPeriod` | Lookback period of the CCI indicator. | 14 |
| `CandleType` | Time frame of the candles processed by the strategy. | 15-minute candles |

## Notes

- The CCI threshold remains constant at `+1` and `-1` just like the MQL source, so trades trigger only after a clear three-step pattern.
- Because risk-based volume sizing relies on instrument metadata (`PriceStep`, `StepPrice`, `VolumeStep`, etc.), ensure those values are available from the connected board.
- The strategy draws candles, the CCI indicator line, and executed trades on the chart for easier visual debugging.
