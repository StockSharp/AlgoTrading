# Pending Order Grid Strategy

This strategy reproduces the behaviour of the MetaTrader "AntiFragile" pending order grid expert advisor. It continuously builds a symmetric grid of stop orders around the current market price and applies protective exits once positions are opened.

## Core logic
- At start-up the strategy caches the best bid and ask from level-1/order book data and places buy-stop orders above price and sell-stop orders below price.
- Order prices are offset from the market by the *Distance* parameter and each subsequent level is spaced by *Spacing (ticks)* multiplied by the instrument price step.
- Every new grid level increases order volume by *Volume Increase %* relative to the starting size, implementing the martingale-style scaling from the MQL version.
- When an order is filled the resulting net position is protected with stop-loss and take-profit orders. Optional trailing stop logic reuses the latest bid/ask to tighten the stop when unrealised profit exceeds the trailing distance.
- The grid is rebuilt automatically after all pending orders have been filled or cancelled and the position returns to flat.

## Parameters
- **Starting Volume** – lot/contract size for the first pending order. Subsequent orders scale by *Volume Increase %*.
- **Volume Increase %** – percentage increment added to each new grid level (0.1 equals +0.1% per level).
- **Distance** – absolute price offset added before the first order (interpreted in instrument currency).
- **Spacing (ticks)** – number of price steps between consecutive grid orders.
- **Orders per side** – maximum number of grid orders for longs and shorts separately.
- **Take Profit (ticks)** – distance of the profit target from the average entry, expressed in price steps.
- **Stop Loss (ticks)** – stop distance from the average entry. Set to zero to disable the initial stop.
- **Trailing Stop (ticks)** – trailing distance. Set to zero to disable trailing adjustments.
- **Enable Long Grid / Enable Short Grid** – toggles for placing buy-stop or sell-stop orders.

## Implementation notes
- StockSharp strategies use net positions, therefore opposing fills will offset each other instead of creating hedged baskets as in MT4. The grid remains symmetric but only the net exposure is tracked.
- Volumes and prices are rounded to the instrument step sizes before submitting orders.
- Trailing stops are recreated by cancelling the previous stop order and sending a new one at the tighter level once profit exceeds the trailing distance.
- The strategy requires order book data (SubscribeOrderBook) to drive price tracking and trailing logic.

## Usage tips
1. Configure *Starting Volume* and *Volume Increase %* conservatively; the original defaults assume Forex lot sizing and can grow quickly.
2. Ensure the portfolio supports stop orders for the target venue. All grid entries are stop-market orders.
3. Monitor margin requirements because a large number of pending orders can consume reserved capital.
