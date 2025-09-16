# Hidden Stop Loss Take Profit Manager Strategy

## Overview
This strategy reproduces the behaviour of the MetaTrader expert "HiddenSLandTP". Instead of
placing visible protective orders on the exchange, it keeps track of hidden stop-loss and
take-profit levels in memory. Whenever the best bid/ask price reaches one of those hidden
levels, the strategy closes the position at market price. The logic is useful when you do not
want to expose your protective levels to the market or the broker.

The implementation relies on the StockSharp high-level strategy API:

* The strategy monitors the current position and calculates hidden protection levels when a
  position is opened or resized.
* Market depth data (best bid and best ask) is used to evaluate hidden stop-loss and take-profit
  breaches with tick accuracy.
* When a breach is detected, the strategy issues `ClosePosition()` to flatten the position and
  logs a descriptive message.
* Optional horizontal lines are plotted on the chart to visualise the hidden levels.

## Parameters
| Name | Description |
| --- | --- |
| `StopLossPoints` | Distance of the hidden stop-loss expressed in price steps. The value is multiplied by `Security.PriceStep`. |
| `TakeProfitPoints` | Distance of the hidden take-profit expressed in price steps. |
| `ReferencePrice` | Base price used for calculating the offsets. `OpenPrice` uses the average entry price of the current position, while `MidPrice` uses the mid-price from the best bid/ask if available. |
| `DrawLines` | Enables the drawing of horizontal lines to display the current hidden stop-loss and take-profit levels. |

All parameters are available for optimisation; the default configuration matches the original
expert advisor (50 points for both stop and take).

## Execution Flow
1. **Initialisation** – on start the strategy resets cached values and subscribes to the order
   book in order to receive real-time best bid/ask updates.
2. **Hidden level calculation** – when the net position becomes non-zero, the strategy computes
   the hidden stop-loss and take-profit based on the selected `ReferencePrice` and the configured
   distances. Levels are rounded to the price step of the instrument.
3. **Monitoring** – each order book update refreshes the cached bid/ask and checks whether the
   hidden levels are breached. The bid is used for long positions and the ask for short
   positions, reproducing the original MetaTrader logic.
4. **Exit handling** – if a hidden level is reached, a single call to `ClosePosition()` is sent.
   Subsequent checks are ignored until the position is confirmed flat.
5. **Completion message** – once all tracked positions are closed the strategy logs
   "Task complete - all tracked positions have been closed." to inform the operator that no
   hidden orders remain active.

## Visualisation
When `DrawLines` is enabled the strategy draws horizontal lines for the hidden stop-loss and
hidden take-profit. Each time the levels are recalculated the lines are redrawn, allowing you to
track the current protection directly on the chart.

## Logging and Diagnostics
* Every recalculation logs the new hidden levels together with the side of the position.
* Breaches log whether the take-profit or stop-loss was hit and the price that triggered the
  closure.
* The completion message is emitted only once after all tracked positions are closed.

## Notes
* The strategy assumes that all trades are executed through the StockSharp strategy instance so
  that `Position.AveragePrice` reflects the effective entry price.
* If `ReferencePrice` is set to `MidPrice` but no best bid/ask is available yet, the calculation
  gracefully falls back to the average position price until real quotes arrive.
* No actual stop or limit orders are placed at the exchange—risk management is entirely internal,
  so reliable data feeds are required to avoid delayed exits.
