# Reversing Martingale Strategy

## Overview
The **Reversing Martingale Strategy** is a direct C# port of the MetaTrader expert advisor “Reversing Martingale EA”. It continuously maintains a single market position and alternates the trade direction after each closed deal. Losing trades trigger a martingale volume progression, while profitable trades reset the cycle back to the initial lot size. All positions are protected by symmetric stop-loss and take-profit levels expressed in price points.

The strategy does not rely on indicators or market structure. It simply reacts to completed positions and keeps capital exposure active at all times (unless trading is disabled).

## Core Logic
1. **Initial setup**
   - When the strategy starts it immediately submits a market order using the `Start Volume` parameter and the configured `First Trade Side`.
   - Protective stop-loss and take-profit orders are attached using the distance specified in `Target (points)`.
2. **Position management**
   - Only one position can be open at a time. The strategy waits until the current position is fully closed by its protective orders or by external actions.
   - After each exit the strategy flips the trade direction (buy → sell or sell → buy).
   - If the last trade realized a loss, the next order volume equals the previous position size multiplied by `Lot Multiplier`. Otherwise, the volume resets to `Start Volume`.
3. **Cycle continuation**
   - Once the new volume and direction are determined, the next market order is submitted immediately, keeping the alternating martingale cycle running.

## Parameters
| Name | Description |
| --- | --- |
| **Start Volume** | Initial trade volume used at the beginning of every winning cycle. |
| **Lot Multiplier** | Volume multiplier applied after a losing trade. Must be greater than 1. |
| **First Trade Side** | Direction of the very first trade when the strategy session starts. |
| **Target (points)** | Distance in price steps used for both stop-loss and take-profit orders. |
| **Order Comment** | Optional text tag assigned to each generated market order. |

## Additional Notes
- The price step distance is converted into `UnitTypes.Step` and passed to `StartProtection`, so both stop-loss and take-profit are always active.
- Volume adjustments respect the security volume step, minimum, and maximum bounds through the `NormalizeVolume` helper.
- The strategy expects trade execution events from the connector; if trading is paused or the connector is offline, the martingale cycle will resume once trading is allowed again.
