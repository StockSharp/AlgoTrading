# Urdala Trol Hedging Grid Strategy

## Overview
The **Urdala Trol Hedging Grid Strategy** is a direct conversion of the MetaTrader 5 expert advisor `Urdala_Trol.mq5` into the StockSharp high-level API. The strategy continuously maintains exposure in both directions and scales positions using a martingale-like grid when stops are hit. It operates entirely on Level1 data (best bid/ask) without any indicators.

## Trading Logic
1. **Initial hedge (Step 0)** – when there are no active positions, the strategy immediately opens one long and one short market order using the *Base Volume* parameter.
2. **Losing-side scale-in (Step 1.2)** – if only one direction remains open and the worst losing position on that side is at least `Grid Step` pips away from the current price, the strategy opens an additional position in the same direction. The new volume equals the volume of the least profitable position plus `Min Lots Multiplier * minVolumeStep`, where `minVolumeStep` is derived from the security's `VolumeStep` or `MinVolume`.
3. **Stop-loss handling (Step 1.1)** – when a position is closed by the stop-loss (including trailing adjustments) with a negative result, the strategy re-enters in the same direction unless there is already a live trade closer than `Min Nearest` pips to the exit price.
4. **Profitable stop reaction (Step 2.1)** – when the stop closes a position with profit, the strategy immediately opens a trade in the opposite direction with the scaled volume.
5. **Trailing stop** – once price advances by `Trailing Stop + Trailing Step` pips beyond the entry, the stop is trailed to keep a distance of `Trailing Stop` pips. Trailing is optional and enforced only when both parameters are greater than zero.

All distances expressed in pips are converted to absolute price offsets through the security's `PriceStep`. For five- or three-digit quotes, the conversion multiplies the step by ten to match the original MQL "adjusted point" logic.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `BaseVolume` | 0.1 | Initial lot size used to open the first hedge pair. |
| `MinLotsMultiplier` | 3 | Number of minimum lots added to the losing trade volume when scaling. |
| `StopLossPips` | 50 | Stop-loss distance in pips. A value of zero disables the stop and trailing logic. |
| `TrailingStopPips` | 5 | Trailing stop distance in pips. Set to zero to disable trailing. |
| `TrailingStepPips` | 5 | Additional pip distance required before the trailing stop moves. Must be positive when trailing is enabled. |
| `GridStepPips` | 50 | Minimum price distance (in pips) between the losing position and the current price before a new scale-in order is placed. |
| `MinNearestPips` | 3 | If an existing position is closer than this distance to the last stop price, the strategy skips the immediate re-entry. |

## Implementation Notes
- Uses `SubscribeLevel1()` to track bid/ask updates and run the decision engine on every tick.
- Orders are registered via the high-level `RegisterOrder` helper, allowing precise tracking through `OnOwnTradeReceived`.
- Individual position objects are managed internally to reproduce hedged behaviour, because StockSharp portfolios are net-position based by default.
- Stop-loss and trailing logic are executed inside the strategy by sending market orders once the thresholds are breached; no native stop orders are registered.

## Usage Tips
1. Assign a liquid instrument and portfolio to the strategy and ensure that `PriceStep`, `VolumeStep`, and min/max volume values are configured for accurate conversions.
2. Start the strategy; it will instantly build a hedged pair and then react to stop events according to the original MQL logic.
3. Adjust pip parameters to align with the instrument's volatility. Large `Grid Step` values reduce the frequency of additional orders, while larger `Min Lots Multiplier` accelerates martingale growth.
4. Monitor the resulting exposure carefully; the martingale behaviour can escalate volume quickly when multiple stops are hit consecutively.

Python implementation is intentionally not provided in this folder, matching the requirements for this conversion task.
