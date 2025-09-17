# Close Basket Pairs Strategy

The **Close Basket Pairs Strategy** automates the MetaTrader script "Close Basket Pairs" inside StockSharp. It continuously monitors a basket of currency pairs together with the direction of the position that must be controlled. Whenever a matching position reaches the configured floating profit or loss threshold, the strategy submits a market order in the opposite direction to close the entire exposure.

Unlike the original one-off script, the StockSharp port runs as a persistent strategy: the basket is evaluated every second, allowing the tool to react immediately after account profit changes. The strategy is designed for manual traders who manage correlation baskets and need a safety net that secures profits or caps losses without watching the terminal all the time.

## How it works

1. The trader defines a comma separated list of basket entries in the form `SYMBOL|SIDE` (for example `EURUSD|BUY,GBPUSD|SELL`).
2. On start the strategy parses the list, validates every entry and matches it against the positions available in the connected portfolio.
3. Every second the strategy scans the portfolio:
   - If a position matches an entry (same symbol and direction) and its floating profit is **greater than or equal to** `ProfitThreshold`, a market order closes the position to lock in the gain.
   - If the floating profit is **less than or equal to** the negative value of `LossThreshold` (the parameter must be negative), the position is closed to stop further losses.
4. Only one exit order per instrument is allowed at a time. Existing protective orders are respected to avoid duplicate exits.

If both thresholds are set to their defaults (`0`), the strategy stops automatically because there is nothing to monitor.

## Parameters

| Parameter | Description | Default |
| --- | --- | --- |
| `BasketPairs` | Comma separated basket definition. Each entry must follow `SYMBOL|SIDE` where `SIDE` is `BUY` or `SELL`. | `EURUSD|BUY,GBPUSD|SELL,USDJPY|BUY` |
| `ProfitThreshold` | Minimum floating profit (in portfolio currency) that triggers an exit. Set to zero to disable profit-based closing. | `0` |
| `LossThreshold` | Negative floating loss (in portfolio currency) that triggers an exit. Use negative numbers (e.g. `-150`). Set to zero to disable loss-based closing. | `0` |

## Usage tips

- Make sure the symbols in `BasketPairs` exactly match the identifiers used by your data feed or broker. The strategy checks the full security identifier first and then falls back to the short symbol code.
- If only the profit or loss guard is required, leave the other threshold at `0`.
- Because the strategy fires market orders, ensure that the portfolio has permission to trade every symbol in the basket.
- The strategy does not cancel existing protective orders. If a manual stop-loss is already active, the tool will simply avoid sending a duplicate exit.

## Differences from the MetaTrader script

- The StockSharp version runs continuously and evaluates positions every second instead of closing them only once when the script is executed.
- It performs extensive validation of the basket definition and logs clear error messages when entries are malformed.
- Order submission uses StockSharp high level helpers (`BuyMarket` / `SellMarket`) while preserving the original profit/loss semantics.
