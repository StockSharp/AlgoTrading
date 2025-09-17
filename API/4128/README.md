# Trade Protector

## Overview
Trade Protector is the StockSharp port of the MetaTrader 4 expert advisor `trade_protector-1_2`. The strategy does not open trades on its own; instead, it continuously monitors the current position and reshapes protective levels in real time. It applies an initial stop-loss for newly opened exposure, transitions to a fixed trailing stop, and later switches to a proportional stop that locks in a configurable fraction of the floating profit. In addition, an escape module can arm a take-profit after a deep drawdown to exit the trade on the next bounce.

The implementation relies on level-1 market data (best bid / best ask) so it can mimic the tick-level behaviour of the original advisor. Protective orders are simulated internally: when a stop or escape take-profit level is hit, the strategy closes the remaining position with a market order.

## Strategy logic
### Baseline stop management
* **Initial stop** – when the net position switches from flat to long or short, the strategy applies an initial stop distance expressed in pips. If the position already had a stop, the stored level is used as the starting point.
* **Trailing before activation** – as long as the floating profit remains below the proportional threshold, the stop trails the price at a fixed distance (`TrailingStopPips`). The stop never moves backwards.

### Proportional trailing
* After the profit exceeds `ProportionalActivationPips`, the strategy keeps a fraction of the realised gain. The protected fraction is controlled by `ProportionalRatio`. For example, a ratio of `0.35` keeps 35% of the current profit while allowing the remaining 65% to fluctuate.
* The spread between the best ask and best bid is subtracted (for long positions) or added (for short positions) to avoid overly optimistic levels.

### Escape module
* When enabled, the escape logic observes the adverse excursion relative to the entry price.
* For longs, if the bid price drops below `EntryPrice - EscapeTriggerPips - 5 points`, an escape take-profit is armed at `EntryPrice + EscapeTakeProfitPips`. Shorts behave symmetrically.
* The armed take-profit can be above or below the entry price. Negative escape distances are supported for controlled-loss exits.
* Once the escape target is reached, the position is closed immediately. The escape target is automatically reset once the position returns to flat.

### Exit execution
* Long stops are triggered when the best bid touches the stored level; short stops fire when the best ask reaches the threshold.
* Escape targets for long positions monitor the best bid, whereas short escape targets track the best ask.
* Every exit sends a market order sized to the absolute position value. A guard flag prevents duplicate orders while the previous exit is still being processed.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `EnableLogging` | `bool` | `true` | Writes informational log entries whenever the stop or escape levels change. |
| `InitialStopPips` | `decimal` | `15` | Distance in pips applied to fresh positions. Set to `0` to keep the existing stop untouched. |
| `TrailingStopPips` | `decimal` | `35` | Static trailing distance used before the proportional mode becomes active. |
| `ProportionalActivationPips` | `decimal` | `12` | Profit threshold (in pips) that enables the proportional stop. A value of `0` activates the proportional mode immediately. |
| `ProportionalRatio` | `decimal` | `0.35` | Fraction of the floating profit that remains protected after activation. Values between `0` and `1` replicate the original behaviour. |
| `UseEscapeMode` | `bool` | `false` | Enables the escape module that arms a take-profit after a deep adverse move. |
| `EscapeTriggerPips` | `decimal` | `0` | Magnitude of the adverse excursion (in pips) required before the escape take-profit is armed. |
| `EscapeTakeProfitPips` | `decimal` | `35` | Distance of the escape take-profit relative to the entry price. Negative values accept a controlled loss. |

## Implementation notes
* Pip size is derived from the instrument price step. Symbols with three or five decimal places use a pip equal to ten minimal price steps to mirror MetaTrader’s definition.
* The strategy stores the latest fill price per side and refreshes the average entry price whenever `PositionPrice` is available, ensuring partial fills are handled gracefully.
* Level-1 data must be available; without best bid/ask updates the trailing logic will not engage.
* Only the C# version is provided. There is no Python implementation or folder for this strategy.

## Differences from the original advisor
* Protective orders are simulated locally. Instead of modifying server-side stop-loss and take-profit values, the strategy submits market exits when levels are breached.
* Spread information comes from live level-1 quotes. On venues where only trades are broadcast, the proportional stop reverts to using the stored bid price without a spread adjustment.
* The escape logic automatically resets once the position returns to flat, removing the need for manual global variables that were used in the MetaTrader code.
* Logging leverages the StockSharp infrastructure (visible in the strategy logs) instead of writing CSV files to disk.
