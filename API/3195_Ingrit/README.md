# Ingrit

## Overview
Ingrit is a conversion of the MetaTrader 5 expert advisor `Ingrit.mq5`. The strategy watches five-minute candles and reacts when a strong counter-trend candle is followed by a wide breakout measured against a swing from fourteen bars ago. Orders are placed at market with configurable stop-loss, take-profit, and trailing stop distances expressed in pips. Signals may optionally be reversed or forced to flatten the opposite exposure before entering a new trade.

## Strategy logic
### Breakout detection
* The strategy processes only finished candles from the selected timeframe (defaults to 5 minutes).
* For a **long** setup the previous candle must close bearish and the distance between the high of the candle 14 bars back and the low of the previous candle must exceed `StepPips` (after converting pips to price units).
* For a **short** setup the previous candle must close bullish and the distance between the high of the previous candle and the low of the candle 14 bars back must exceed `StepPips`.
* Enabling `ReverseSignals` swaps the long and short conditions, recreating the optional reversal mode from the original robot.

### Trade management
* Market orders are sent using the strategy `Volume`. When `CloseOppositePositions` is enabled the requested size is increased by the absolute value of the current position so that reversals close the existing exposure in the same trade.
* A fixed stop-loss and take-profit (if greater than zero) are attached immediately after entry. Both distances are converted from pips using the security price step and automatically adapt to three- and five-digit FX quotes.
* The trailing stop becomes active once unrealized profit exceeds `TrailingStopPips + TrailingStepPips`. For longs the stop trails below the close, for shorts it trails above the close. Each update keeps the stop at least `TrailingStepPips` away from the previous trailing level to avoid rapid modifications.

### Additional behaviour
* Trailing can be disabled by setting `TrailingStopPips` to zero. If trailing is active the step must remain positive (the strategy performs the same validation as the MQL version).
* All calculations run on completed candles; no intrabar processing is required in StockSharp.
* The strategy does not create pending orders—every signal is executed with a market order and the protective levels are simulated internally.

## Parameters
| Parameter | Description |
| --- | --- |
| `CandleType` | Timeframe used to build candles for the breakout logic. Default: 5-minute time frame. |
| `StopLossPips` | Stop-loss distance in pips. A value of `0` disables the fixed stop. |
| `TakeProfitPips` | Take-profit distance in pips. A value of `0` disables the fixed target. |
| `TrailingStopPips` | Base trailing stop distance in pips. Set to `0` to disable trailing. |
| `TrailingStepPips` | Extra pip distance that must be gained before the trailing stop moves again. Must be positive when trailing is enabled. |
| `StepPips` | Minimum swing distance in pips between the reference candle and the latest candle before a signal triggers. |
| `ReverseSignals` | If `true`, swaps long and short conditions (reverse mode). |
| `CloseOppositePositions` | If `true`, enlarges the market order to flatten any opposite exposure before opening the new position. |
| `Volume` | Strategy property that defines the base order size. Combine with `CloseOppositePositions` to control reversal behaviour. |

## Notes
* Pip values are derived from the security price step. When the instrument uses three or five decimal places the strategy multiplies the step by ten so that one pip equals the standard FX definition.
* There is no Python version for this strategy in the repository.
