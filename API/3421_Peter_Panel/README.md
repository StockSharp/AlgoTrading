# Peter Panel Strategy

The **Peter Panel Strategy** ports the discretionary MetaTrader 5 control panel "Peter Panel" into StockSharp. The original expert advisor drew three horizontal lines (entry, take-profit, and stop-loss) and a button matrix that let the trader instantly submit market or pending orders using those levels. This C# strategy keeps the decision flow intact while replacing the graphical panel with interactive strategy parameters. Every toggle behaves like the original button: setting the parameter to `true` performs the action immediately and the flag resets back to `false`.

## Key Concepts

1. **Manual assistant** – the strategy does not generate signals. You decide when to trade by toggling the parameters exposed in the strategy UI or automation scripts.
2. **Shared price lines** – the aqua entry line, green take-profit line, and red stop-loss line are represented by three decimal parameters. Their values can be set manually or recalculated around the current mid price via the `ResetCommand` toggle.
3. **Comprehensive order coverage** – all six order types from the panel are implemented: market buy/sell, buy stop, buy limit, sell stop, and sell limit. Protective orders are attached after each fill, emulating the TP/SL fields that the MetaTrader panel populated automatically.
4. **Bulk modifications** – the `ModifyCommand` parameter re-applies the current price lines to every active pending order and to the protective stop-loss/take-profit orders of the open position.
5. **One-touch liquidation** – the `CloseCommand` button cancels outstanding pending orders, removes protective orders, and flattens the net position at market.

## Original vs. StockSharp Implementation

| Feature | MetaTrader 5 Peter Panel | StockSharp Peter Panel Strategy |
| --- | --- | --- |
| User interface | On-chart dialog with buttons and editable fields | Strategy parameters that behave like switches and numeric inputs |
| Entry/TP/SL manipulation | Drag horizontal lines or press "Reset" to re-center | Edit parameter values directly or use the `ResetCommand` toggle |
| Order submission | Button triggers synchronous `OrderSend` request | Parameter toggle calls the corresponding `Buy/Sell` helper and stores order references |
| TP/SL handling | Filled through `MqlTradeRequest.tp` and `.sl` in every order | Protective stop and target are registered as separate stop/limit orders immediately after fills |
| Order modification | Select a ticket in the list and press "Modify" | `ModifyCommand` cancels/replaces every active pending order and refreshes protective orders |
| Order closing | Press "Close" on the highlighted ticket | `CloseCommand` closes the whole position and cancels all pending and protective orders |
| Order list | Graphical table of tickets and levels | Strategy relies on StockSharp order tracking; detailed status is available in the logs |

> **Note:** MetaTrader allowed the trader to select a single ticket from the list. The StockSharp port applies modifications and closures to every order created by the strategy because a direct single-ticket selection is not available inside strategy parameters.

## Parameters

| Parameter | Description |
| --- | --- |
| `Volume` | Trade volume in lots. It is validated against the security volume step and min/max limits. |
| `EntryLevel` | Price used for pending orders (aqua line). |
| `TakeProfitLevel` | Green line price. It acts as the take-profit level for long trades and as the protective stop level for short trades, mirroring the original panel. |
| `StopLossLevel` | Red line price. It acts as the protective stop for long trades and as the take-profit target for short trades. |
| `BuyMarketCommand` | Submit a market buy order when set to `true`. The flag resets to `false` after the order is sent. |
| `BuyStopCommand` | Place a buy stop order at `EntryLevel`. |
| `BuyLimitCommand` | Place a buy limit order at `EntryLevel`. |
| `SellMarketCommand` | Submit a market sell order. |
| `SellStopCommand` | Place a sell stop order at `EntryLevel`. |
| `SellLimitCommand` | Place a sell limit order at `EntryLevel`. |
| `ModifyCommand` | Re-apply `EntryLevel`, `TakeProfitLevel`, and `StopLossLevel` to existing pending orders and to the protective orders of the current position. |
| `CloseCommand` | Cancel pending orders, remove protective orders, and flatten the position at market. |
| `ResetCommand` | Recalculate the three price levels around the current bid/ask midpoint. |

## Workflow

1. Start the strategy once the desired security and portfolio are connected. The level 1 subscription updates the internal bid/ask cache that powers the `ResetCommand` function.
2. Use the `ResetCommand` toggle or manual edits to configure the aqua, green, and red price levels.
3. Trigger a trade by toggling one of the action parameters to `true`. The strategy automatically resets the toggle to `false` so the next activation is intentional.
4. After fills, the strategy submits the appropriate stop-loss and take-profit orders based on the direction of the position. For example, a long position gets a sell stop at the red line and a sell limit at the green line, while a short position receives the inverse combination.
5. Modify the levels at any time and press `ModifyCommand` to refresh pending orders and protective exits without restarting the strategy.
6. When the trading session is over, toggle `CloseCommand` to flatten and clean up every order managed by the strategy.

## Differences from the Original Panel

- There is no graphical list of tickets. Instead, StockSharp logs keep track of every registered order and trade. You can connect the strategy to any external UI if individual ticket management is required.
- Stop-loss and take-profit values are implemented as explicit child orders because StockSharp cannot embed TP/SL prices directly into the main order request. The behaviour matches the final result of the MetaTrader panel: the position ends up protected by the same levels.
- Order replacement is performed through cancel-and-recreate cycles. This keeps the workflow deterministic even on venues that do not support in-place modifications.

## Usage Tips

- Combine the strategy with StockSharp charts or dashboards to recreate the original panel experience, replacing the on-chart buttons with UI elements that toggle the exposed parameters.
- The strategy does not queue multiple actions. If you need to automate sequences (for example, reset levels and then place a pending order), trigger the toggles sequentially after the previous one resets to `false`.
- Protective orders are only created for non-zero positions. If you place pending orders without a position, call `ModifyCommand` after the order fills to ensure the latest levels are applied.

## Safety Considerations

- Always verify that the portfolio, security, and price step information are available before submitting any order. The strategy logs warnings when required data is missing.
- The `Volume` parameter is clamped to the instrument limits. If the adjusted volume becomes zero because of an incompatible step or min volume, no order is sent and a warning appears in the log.
- When `CloseCommand` is executed the strategy first cancels protective orders, then pending orders, and finally flattens the position. This mirrors the defensive order of operations from the original expert advisor.
