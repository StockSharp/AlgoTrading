# Nevalyashka Strategy

This strategy implements a simple alternating long/short system with martingale position sizing.

## Strategy Logic

1. At start a short position is opened.
2. A fixed take profit and stop loss are attached to the position.
3. Whenever the position is closed (by stop or target):
   - The next trade is opened in the opposite direction.
   - If the previous trade ended with a loss, the order volume is multiplied by `LotMultiplier`.
   - If the previous trade ended with a profit, the volume resets to the base `Volume`.
4. Steps 2‑3 repeat indefinitely.

## Parameters

- `Volume` – base order volume used for the first trade and after winning trades.
- `LotMultiplier` – multiplier applied to the volume after a losing trade.
- `TakeProfit` – profit target distance in price points.
- `StopLoss` – stop loss distance in price points.

## Notes

- Protective orders are handled through `StartProtection`.
- The strategy does not rely on market data; it reacts only to position changes.
