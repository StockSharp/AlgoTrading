# Close All MT5 Strategy

The **Close All MT5** strategy reproduces the behaviour of the MQL utility that closes
positions once a floating profit target is met. It continuously monitors the portfolio
and flattens long or short exposure when the configured threshold is reached. A manual
trigger simulates the original chart button so the operator can immediately close
matching trades and cancel pending orders.

## Operation

* Subscribes to tick data for every security that currently has an open position.
* Tracks the latest traded price and compares it with the average entry price of each
  position.
* Evaluates the floating profit in money, points or pips depending on the selected
  unit.
* Issues market orders to close eligible positions when the target profit (or loss) is
  crossed.
* Provides a manual trigger that can close positions on demand according to the chosen
  mode (all positions, longs only, shorts only, filtered by symbol, etc.).

## Parameters

| Name | Description |
| ---- | ----------- |
| `Position Filter` | Determines whether automatic checks monitor long positions, short positions, or both. |
| `Profit Mode` | Measurement unit for the floating profit (`Money`, `Points`, `Pips`). |
| `Profit Threshold` | Target profit (positive) or loss (negative) that triggers position closing. |
| `Close Mode` | Manual close behaviour that emulates the original chart button. |
| `Comment Filter` | Optional substring that must appear inside the originating strategy identifier before a position is processed. |
| `Currency Filter` | Symbol identifier used by `CloseCurrency` when `Close Mode` is triggered. Leave empty to use the current strategy security. |
| `Magic Number` | Strategy identifier used by `CloseMagic`. Matches against the internal strategy id associated with the position. |
| `Ticket Number` | Position identifier processed when `CloseTicket` is selected. |
| `Max Slippage` | Maximum acceptable slippage in price steps. Reserved for future enhancements. |
| `Trigger Close` | Set to `true` to execute the manual close routine; resets automatically afterwards. |

## Notes

* Profit evaluation prefers the platform-reported unrealised P&L and falls back to a
  manual calculation based on price steps when necessary.
* The `Comment Filter` and `Magic Number` rely on the strategy identifier associated
  with a position. This mirrors the comment and magic number checks used in the MQL
  version.
* `Close Mode` set to `CloseAllAndPending` cancels active orders whose comments match
  the filter after closing the positions.
