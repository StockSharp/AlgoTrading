# SpaceX Delete StopLoss TakeProfit Button Strategy

## Overview
This strategy reproduces the **"DELETE SL_TP"** button from the original MetaTrader panel *SpaceX_Delete_StopLoss_TakeProfit_button.mq5*. It is designed as a utility helper that scans the current portfolio and cancels all active protective stop-loss or take-profit orders that belong to open positions. The conversion targets the StockSharp high-level API and provides a convenient way to remove protective brackets without manually opening each ticket.

The strategy does not open or close positions on its own. It simply inspects the already open trades and removes their protective orders when instructed to do so. This makes it suitable for traders who manage positions manually or via other automated systems but want a quick panic button that clears all stop and take-profit orders.

## Original Expert Advisor
The MetaTrader version draws a single dialog window with a **DELETE SL_TP** button. Whenever the button is pressed the expert iterates through all open positions and calls `PositionModify` with zero values for stop-loss and take-profit. As a result every protective level is detached from the position while the position volume stays untouched.

Key behaviours of the source code:

* No market entries or exits are created.
* All symbols in the terminal are processed without filtering.
* Only stop-loss and take-profit values are removed; order comments and magic numbers remain intact.
* The action is triggered exclusively by the GUI button.

## StockSharp Implementation
The StockSharp conversion keeps the behaviour focused on removing protective orders. Instead of a GUI dialog the action is driven by strategy parameters that can be toggled from the StockSharp UI or from code. The strategy works with any broker adapter that exposes order stop or take-profit information.

Two execution modes are supported:

1. **Automatic execution on start** – optional. When enabled the strategy removes protective orders immediately after it starts running.
2. **Manual command** – a boolean parameter that mimics the original button. Setting the parameter to `true` schedules a clean-up on the next timer tick, after which the flag resets to `false`.

The conversion cancels protective orders by calling `CancelOrder` on every active order that is identified as stop-loss, take-profit, or any other conditional protection order. Position volumes are never touched.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| **Run On Start** (`ApplyOnStart`) | When `true` the strategy removes protective orders immediately after the strategy starts. | `true` |
| **All Securities** (`AffectAllSecurities`) | Processes all portfolio positions. When `false` only the strategy security is considered. | `true` |
| **Delete Request** (`DeleteRequest`) | Manual trigger that emulates the MetaTrader button. Flip it to `true` to perform a one-off removal; it resets automatically. | `false` |
| **Polling Interval (s)** (`PollingIntervalSeconds`) | Timer interval in seconds used to poll the manual trigger. The timer also executes the delete request when `Run On Start` is disabled. | `1` |

## How It Works
1. On start the strategy validates the polling interval and starts a timer that wakes up every *N* seconds.
2. If **Run On Start** is enabled an immediate clean-up is executed.
3. Every timer tick checks the **Delete Request** flag. When the flag is `true` the strategy collects the securities that have open positions inside the configured scope and cancels all protective orders for those instruments.
4. After execution the manual flag is reset to `false`, ensuring the action runs only once per request.

### Identifying Protective Orders
An order is treated as protective when any of the following conditions is met:

* The order type is `Stop`, `TakeProfit`, or `Conditional`.
* A stop price, take-profit price, or non-null order condition is present.

This conservative definition covers the most common adapters. If a connector uses custom order types or conditions for stop management, extend the detection logic accordingly.

## Usage Tips
* Attach the strategy to the connector that manages your open trades. Make sure all positions you want to manage are visible to the configured portfolio.
* Trigger the delete request from the parameter grid in Hydra or Terminal by toggling the **Delete Request** checkbox.
* Combine the utility with other strategies to temporarily remove protective brackets before applying new ones.
* Keep the polling interval small (1 second by default) for a responsive button experience. Increase it if you want to reduce timer activity.

## Differences Compared to the Original EA
* The MetaTrader button acts instantly via a chart dialog. In StockSharp the action is exposed as a parameter monitored by a timer.
* Protective orders are cancelled instead of modifying position objects. This is the natural approach within StockSharp because stop-loss and take-profit levels are represented as separate orders rather than inline position properties.
* Optional scope control allows limiting the operation to the attached security, which is an extra convenience compared to the original expert.

## Limitations
* The strategy requires that the adapter exposes stop-loss and take-profit orders as active orders. If the broker uses server-side protective levels that are not represented as orders, cancelling them might not be possible.
* No GUI dialog is created. Control is performed entirely via strategy parameters or programmatic access.
* The utility does not recreate protective levels; it only removes them.

## Testing
The strategy does not include dedicated automated tests because it performs utility functions without complex calculations. Manual testing can be performed by opening sample positions, attaching the strategy, and verifying that all protective orders are cancelled after each trigger.
