# Pending Stop Grid Strategy

## Overview
The **Pending Stop Grid Strategy** is a direct conversion of the MetaTrader 4 expert advisor `new.mq4`. The strategy maintains two symmetric ladders of pending orders:

- A sequence of buy stop orders above the current ask price.
- A sequence of sell stop orders below the current bid price.

Each additional level increases both the order distance and traded volume proportionally to its position within the ladder. Stop-loss and take-profit targets are assigned to every order individually.

## Trading Logic
1. The strategy subscribes to Level 1 data and continuously tracks the latest best bid and ask prices.
2. Once market data and trading permissions are available, it computes the pip size using the security price step (with five-digit and three-digit symbols automatically normalised to standard pip values).
3. Before placing orders, the strategy validates that the configured base volume respects the instrument's minimum and maximum volume constraints.
4. For each index `i` from 1 to `NumberOfTrades`:
   - The order volume is calculated as `BaseVolume * i` and rounded to the nearest allowed step.
   - A buy stop is placed at `Ask + DistancePips * i * pipSize` with optional stop-loss and take-profit offsets.
   - A sell stop is placed at `Bid - DistancePips * i * pipSize` with mirrored stop-loss and take-profit offsets.
5. If any order is filled, cancelled or rejected, the corresponding slot in the ladder is cleared and immediately replenished with a new pending order when market data permits.
6. Built-in `StartProtection()` is called on start to activate the platform risk controls.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `BaseVolume` | Volume of the first pending order. Each subsequent order multiplies this base by its index. | `0.1` |
| `NumberOfTrades` | Number of buy stop and sell stop orders maintained simultaneously. | `10` |
| `DistancePips` | Distance (in pips) between the market price and each pending order level. | `10` |
| `StopLossPips` | Stop-loss distance assigned to every order. Set to zero to disable stop-loss placement. | `10` |
| `TakeProfitPips` | Take-profit distance assigned to every order. Set to zero to disable take-profit placement. | `10` |

All parameters are exposed as optimisable strategy parameters and are validated to avoid negative or zero values (where applicable).

## Additional Notes
- Volumes are rounded to the nearest permissible step and clamped within the exchange-defined minimum and maximum boundaries.
- Prices are normalised with `Security.ShrinkPrice` to respect the instrument tick size.
- The strategy does not hold historical state: it rebuilds the entire ladder whenever the security is reset or trading permissions change.
- The logic avoids manual indicator buffers in favour of the StockSharp high-level API bindings, following the project-wide conversion guidelines.
