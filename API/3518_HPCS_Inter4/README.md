# HPCS Inter4 Strategy (3518)

## Overview

This strategy ports the MetaTrader expert advisor "_HPCS_IntFourth_MT4_EA_V01_WE" to the StockSharp high-level API. The original script immediately opens a long position, applies protective stop-loss and take-profit levels measured in MetaTrader pips, and forcefully closes the trade after a short holding period. The C# version reproduces the same behaviour by combining the built-in protection manager with a one-second timer that monitors the elapsed time since entry.

## Trading Logic

1. **Initialisation**
   - When the strategy starts it computes the MetaTrader pip size from the security `PriceStep` and decimal precision (5- and 3-digit symbols use a 10x multiplier).
   - The high-level `StartProtection` helper is configured with the requested take-profit and stop-loss distances. The stop-loss distance includes the extra buffer that the original EA applies using `OrderModify`.
   - The volume is fixed and comes from the `OrderVolume` parameter.

2. **Entry**
   - A single market buy order is submitted immediately after the strategy is launched. No further entries are placed.
   - Once the first fill is reported the strategy stores the execution time.

3. **Exit**
   - A timer checks the open position every second.
   - When the holding period reaches `CloseDelaySeconds`, the strategy closes the long position with a market sell order if exposure is still positive.
   - Protective stop-loss and take-profit orders are maintained automatically by the protection manager using market exits.

The logic only trades in the long direction, mirroring the behaviour of the MetaTrader script.

## Parameters

| Name | Description | Default | Optimizable |
| --- | --- | --- | --- |
| `OrderVolume` | Fixed volume used when sending the initial market buy order. | `1` | No |
| `StopLossPips` | Base MetaTrader pip distance applied to the initial stop-loss. | `10` | No |
| `ExtraStopPips` | Additional MetaTrader pip buffer subtracted from the stop after entry. | `10` | No |
| `TakeProfitPips` | MetaTrader pip distance of the profit target. | `10` | No |
| `CloseDelaySeconds` | Time in seconds before the position is forcefully closed. `0` disables the timer exit. | `30` | No |

## Implementation Notes

- The pip size helper multiplies the reported `PriceStep` by 10 for 3- and 5-decimal instruments so that parameter values keep the same scale as in MetaTrader.
- `StartProtection` uses `UnitTypes.Price` distances so that protective orders operate with market exits, exactly like the EA did with `OrderClose`.
- `OnNewMyTrade` records the first filled buy trade to start the holding-period countdown and resets the state when the position is fully closed.
- The timer runs at one-second intervals to replicate the original `OnTick` time check while remaining insensitive to market inactivity.
- All code comments are written in English to comply with repository guidelines.
