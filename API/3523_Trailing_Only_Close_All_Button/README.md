# Trailing Only Close All Button Strategy

This strategy reproduces the MetaTrader expert *Trailing Only with Close All Button*. It does not open new trades on its own. Instead, it manages positions created elsewhere by applying stop-loss, take-profit and trailing-stop levels and exposes a manual **Close All** button. Once the button is toggled, the strategy closes or cancels positions and orders according to the configured filters.

## Core behaviour

- Subscribes to **Level1** data to receive the latest bid/ask quotes.
- When a position is opened, the strategy immediately seeds stop-loss and take-profit prices calculated from the MetaTrader-style pip inputs.
- Trailing stops activate after the price moves in favour of the position by at least `TrailingStopPips + TrailingStepPips`. Each additional improvement of `TrailingStepPips` shifts the stop closer to the market.
- If the market reaches the computed stop or take-profit levels the strategy exits using market orders, mirroring how MetaTrader would close the position via server-side stops.
- The `CloseAll` button can flatten positions, cancel orders or do both, using optional symbol and profit filters to match the original script’s flexibility.

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `StopLossPips` | Distance from the entry price to the stop-loss, in MetaTrader pips (0.00010 for 5-digit FX symbols). | 500 |
| `TakeProfitPips` | Distance from the entry price to the take-profit, in MetaTrader pips. | 1000 |
| `TrailingStopPips` | Minimum profit (in pips) required before the trailing logic starts moving the stop. | 200 |
| `TrailingStepPips` | Extra distance (in pips) the price must travel beyond `TrailingStopPips` before each new stop adjustment. Must be > 0 when trailing is enabled. | 50 |
| `ManualCloseMode` | Determines what the button affects: only positions, only orders or both. | Positions |
| `ManualCloseSymbol` | Limits the scope of the button to the current chart symbol or to all instruments traded by the strategy hierarchy. | Chart |
| `ManualCloseProfitFilter` | Optional PnL filter for the manual button: close all, only profitable or only losing positions. | ProfitOnly |
| `CloseAll` | Virtual button. Set to `true` in the UI to trigger the manual closing routine; the strategy resets it back to `false`. | `false` |

## Implementation notes

- Pip distances are converted using the instrument’s `PriceStep`. If the instrument uses 3 or 5 decimals the multiplier `10` is applied to match MetaTrader’s definition of a pip.
- The strategy uses only high-level API calls: `SubscribeLevel1()` to receive quotes and `BuyMarket` / `SellMarket` for exits.
- Trailing and manual close states are reset whenever the net position becomes flat, preventing stale levels from affecting the next trade.
- Manual closing iterates over `Portfolio.Positions` and the strategy’s own order list to decide what to close or cancel, respecting the configured filters.
