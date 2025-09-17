# Locker Hedging Grid

The strategy replicates the MetaTrader 4 expert advisor **Locker.mq4**. It starts every cycle with a market buy and then manages a hedged grid of buy and sell orders. Whenever the combined unrealized profit of all open trades reaches a fixed fraction of the account equity, every position is closed and a fresh cycle begins. If the floating loss exceeds the same fraction in the negative direction, the strategy progressively adds rescue orders at fixed point intervals, locking price swings with alternating long and short entries.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `NeedProfitRatio` | Fraction of portfolio equity that must be earned (or lost) before closing/adding orders. `0.001` corresponds to 0.1% of the account. | `0.001` |
| `InitialVolume` | Volume of the very first market buy order at the beginning of every cycle. | `0.5` |
| `StepVolume` | Volume for each rescue order that is added while the strategy is in a drawdown phase. | `0.2` |
| `StepPoints` | Distance in MetaTrader points between rescue orders. Converted internally into price by using `Security.PriceStep` (pip) information. | `50` |
| `EnableRescue` | Enables the averaging grid when the floating loss breaches the negative threshold. If disabled the strategy only performs the initial trade and waits for profit. | `true` |

## Trading Logic

1. **Cycle start**
   - On the first incoming trade quote a market buy is sent with `InitialVolume`.
   - The entry price becomes the reference checkpoint, and both the highest buy and lowest sell trackers are reset to that price.

2. **Profit locking**
   - At every tick the strategy sums the unrealized P&L from all long and short legs. Long legs contribute `(price - averageBuyPrice) * longVolume`, while short legs add `(averageSellPrice - price) * shortVolume`.
   - Once the floating profit reaches `NeedProfitRatio * equity`, all positions are flattened via opposite market orders. A new cycle starts after fills are confirmed.

3. **Rescue grid**
   - When unrealized profit drops below `-NeedProfitRatio * equity` and `EnableRescue` is true, the system waits for price to move `StepPoints` (converted to price distance). Each new high above the last checkpoint issues another market buy, whereas each new low schedules a market sell. Volumes always equal `StepVolume`.
   - Checkpoint and directional extremes are updated after every rescue order so that the next addition requires another full step in price.

4. **Cycle reset**
   - After both long and short inventories drop to zero (confirmed through own trade notifications) the checkpoint and extremes are reset to the latest trade price and the strategy is ready to seed a new cycle with the initial buy.

## Implementation Notes

- Uses `SubscribeTrades().Bind(ProcessTrade)` to work with tick-by-tick prices, mirroring the original MQL EA that reacted to the current bid/ask.
- Converts MetaTrader "points" to StockSharp prices via a pip size derived from `Security.PriceStep`. Symbols quoted with 3 or 5 decimals receive the standard *x10* adjustment.
- Tracks long and short inventories separately inside `OnOwnTradeReceived`, enabling hedged exposure exactly like the MT4 version (buy and sell positions can coexist).
- Portfolio equity is estimated from `Portfolio.CurrentValue` with fallbacks to `CurrentBalance` or `BeginValue`. The first positive reading is cached so that the profit threshold remains stable even if the provider stops reporting the value.
- Every market order volume goes through an `AlignVolume` helper that honors `Security.VolumeStep`, `VolumeMin`, and `VolumeMax` restrictions.

## Usage Tips

- Ensure that the instrument metadata supplies a correct `PriceStep`; otherwise the point-to-price conversion will be inaccurate and the grid distances will not match MetaTrader behaviour.
- Because the rescue logic mirrors a martingale-style averaging, choose `StepVolume` carefully and monitor risk. Increasing both `StepPoints` and `StepVolume` reduces the number of open trades but amplifies exposure.
- Set `EnableRescue` to `false` to replicate a conservative variant that simply waits for the first position to hit the profit target without ever averaging down.
- Back-testing on Forex symbols should be performed with tick data to match the EA’s original granularity.

## Differences from the MQL Expert

- The original script attempted to close perfectly offsetting order pairs when more than eight trades were active. That block never executed because of a ticket-filter bug, and it has been omitted.
- `StepLot` recalculation based on pre-existing orders at initialization is not replicated; volumes are controlled entirely through the parameters exposed in StockSharp.
- Order comments, alert pop-ups, and manual stop flags from the EA are not present—the StockSharp version focuses purely on autonomous trading logic.
