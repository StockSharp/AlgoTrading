# VR Overturn Strategy

The **VR Overturn Strategy** implements a simple martingale/anti-martingale logic.
It always keeps a single position and, once closed, immediately opens a new one
based on the result of the previous trade.

## Strategy Logic

1. Open the first position according to `StartSide` with volume `StartVolume`.
2. Attach stop-loss and take-profit using point offsets.
3. When the position closes:
   - Calculate profit of the last trade.
   - For **Martingale** mode:
     - After a profitable trade, reset the volume to `StartVolume` and keep the same direction.
     - After a losing trade, multiply the volume by `Multiplier` and reverse the direction.
   - For **AntiMartingale** mode:
     - After a profitable trade, multiply the volume by `Multiplier` and keep the same direction.
     - After a losing trade, reset the volume to `StartVolume` and reverse the direction.
4. Open the next position using the computed direction and volume.

The process repeats indefinitely while the strategy is running.

## Parameters

| Name | Description |
|------|-------------|
| `Mode` | Trading mode: `Martingale` or `AntiMartingale`. |
| `StartSide` | Side of the very first trade (`Buy` or `Sell`). |
| `TakeProfit` | Take-profit value in points from the entry price. |
| `StopLoss` | Stop-loss value in points from the entry price. |
| `StartVolume` | Initial volume used for the first order. |
| `Multiplier` | Multiplier applied to the volume after a profit or loss. |

## Notes

- Protective orders are registered as stop and limit orders.
- Only one position exists at any moment.
- The strategy does not use any market indicators.
