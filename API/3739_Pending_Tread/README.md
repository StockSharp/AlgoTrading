# Pending Tread Strategy

MetaTrader expert **Pending_tread** translated to StockSharp. The strategy constantly maintains two layers of pending orders: one grid above the market and one below. Each layer can switch between buy/sell orders and attaches optional stop-loss and take-profit offsets expressed in MetaTrader pips. A portfolio equity guard replicates the emergency liquidation from the original EA.

## How it works

1. Level 1 data is subscribed to capture the current best bid and ask. Every five seconds (or when data resumes after the throttle window) the strategy evaluates whether new orders are required.
2. For each enabled grid (above or below the market) it counts active orders that were created by the strategy. If fewer than 10 are live, additional orders are placed at multiples of `PipStep` from the current bid/ask.
3. The helper automatically converts MetaTrader pips to absolute prices using the security `PriceStep`. Symbols quoted with 3 or 5 decimals receive the typical ×10 pip multiplier.
4. Stop-loss and take-profit prices are attached directly to the pending order when the distances are positive. Disabling the stop-loss parameter leaves the orders without protective stops, just like the original EA.
5. The optional equity guard stores the starting portfolio value. When the floating equity drops below `MaxLossPercent` of the initial balance, all positions are flattened and every pending order belonging to the grid is cancelled.

## Parameters

| Name | Description |
|------|-------------|
| `PipStep` | Distance between consecutive pending orders expressed in MetaTrader pips. |
| `TakeProfitPips` | Take-profit distance (pips). Set to zero to disable. |
| `StopLossPips` | Stop-loss distance (pips). Set to zero or disable the flag to omit stops. |
| `OrderVolume` | Volume submitted with each pending order after normalization to the instrument volume step. |
| `Slippage` | Placeholder kept for parity with the original EA. StockSharp uses limit/stop prices so the value is informational only. |
| `EnableBuyGrid` / `EnableSellGrid` | Toggles for the above-market and below-market layers. Disabling a layer cancels its outstanding orders. |
| `AboveMarketTradeDirection` / `BelowMarketTradeDirection` | Chooses whether the grid places buy or sell orders for the corresponding side of the market. |
| `EnableStopLoss` | Enables stop-loss generation for pending orders. |
| `MinimumEquity` | Absolute equity threshold. If the current portfolio value is below the level, the strategy pauses order creation. |
| `EnableEquityLossProtection` | Activates the drawdown guard that closes positions and pending orders when the loss threshold is breached. |
| `MaxLossPercent` | Maximum percentage loss from the starting equity before triggering the guard. |

### Fixed behaviour

- Each grid maintains up to **10** pending orders, matching the hard-coded `totalOrdersPerSide` value from MQL5.
- Order submission is throttled to once every five seconds to copy the `lastOrderTime` delay.
- Orders are tagged with comments so that previous directions are cancelled automatically when the user flips the grid side.

## Usage notes

1. Ensure the traded security exposes a valid `PriceStep`; the pip-to-price conversion relies on it.
2. Start the strategy with a connected portfolio so that the initial equity snapshot can be recorded for the guard.
3. Disabling a grid parameter removes any pending orders belonging to that side. Re-enabling it rebuilds the 10-layer ladder from the current market price.
4. Because limit/stop orders include the take-profit and stop-loss prices, no additional protective orders are required once an entry fills.

## Files

- `CS/PendingTreadStrategy.cs` – C# implementation using the high-level Strategy API.
