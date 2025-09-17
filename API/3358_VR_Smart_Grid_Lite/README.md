# VR Smart Grid Lite Strategy

The **VR Smart Grid Lite Strategy** replicates the logic of the MetaTrader expert advisor with the same name. The strategy builds a martingale-style averaging grid using market orders. Position sizing starts from a base volume and doubles each time price moves against the existing position by a user-defined distance. The strategy supports two exit modes: closing the extreme trades at a weighted take-profit price or partially reducing exposure while keeping the grid active.

## Parameters
- **Take Profit (pips)** – distance in pips used to exit when only one position is active.
- **Start Volume** – initial order volume for the first trade in each direction.
- **Maximal Volume** – hard cap for any single order opened by the grid.
- **Close Mode** – `Average` closes the oldest and newest orders at a weighted target; `PartClose` closes part of the newest order and all of the oldest order.
- **Order Step (pips)** – minimum price distance that must be travelled against the position before a new trade is opened.
- **Minimal Profit (pips)** – additional profit margin added to the weighted average exit price.
- **Slippage (pips)** – placeholder parameter retained from the original EA for completeness.
- **Candle Type** – timeframe used to drive decision making (the previous completed candle determines the trading bias).

## Algorithm
1. On every finished candle the strategy evaluates the previous candle direction.
2. If the previous candle closed bullish and either no long trades exist or the price moved down by the configured step, a new **buy market** order is placed.
3. If the previous candle closed bearish and either no short trades exist or the price moved up by the configured step, a new **sell market** order is placed.
4. Volumes are calculated from the lowest priced position in the direction and doubled at each new level, respecting the maximum volume and broker volume steps.
5. When only one position remains, the strategy applies the simple take-profit distance and exits on touch.
6. With multiple positions, the strategy computes weighted averages using the extreme entries:
   - **Average mode** closes both extremes when price reaches the weighted target plus the minimal profit buffer.
   - **PartClose mode** closes a portion of the newest order equal to the start volume and fully closes the oldest order, allowing the grid to keep running with reduced exposure.
7. All filled and closed positions are tracked to keep the internal grid state synchronized with the live portfolio.

## Notes
- The strategy relies on market orders, so real execution quality and slippage depend on broker conditions.
- Ensure that instrument volume constraints (minimum volume and volume step) are compatible with the selected start volume.
- As with any grid or martingale approach, risk can grow quickly when markets trend strongly against the position; use prudent money management.
