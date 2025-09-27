# VR Smart Grid Lite Averaging Strategy

## Overview
The VR Smart Grid Lite Averaging strategy is a grid averaging system that follows the original MetaTrader 5 expert advisor. The algorithm opens market orders in the direction of the most recent bullish or bearish candle and builds a martingale-style ladder whenever price moves against the position. Distances, volumes and exit logic can be tuned to match the original MQL implementation.

## Trading Logic
- On every completed candle the strategy checks its direction.
  - A bullish candle allows a new buy order if the current price is at least `Order Step (pips)` below the lowest existing buy entry.
  - A bearish candle allows a new sell order if the current price is at least `Order Step (pips)` above the highest existing sell entry.
- The first order for each side uses `Start Volume`. Every additional order doubles the volume of the farthest order on that side, while `Max Volume` limits the absolute size.
- When only a single position exists on a side, the trade is closed once price reaches the `Take Profit (pips)` distance.
- With two or more positions the closing logic depends on the selected `Close Mode`:
  - **Average** – closes the highest and lowest orders once price hits their weighted average plus `Minimal Profit (pips)`.
  - **PartialClose** – closes the lowest order entirely and reduces the highest order by `Start Volume` when price reaches the blended target.

## Risk Management
- Volumes are adjusted to the broker’s `MinVolume`, `MaxVolume` and `StepVolume` to avoid rejection.
- The built-in `StartProtection()` call ensures that StockSharp account protection is activated before trading.

## Parameters
| Name | Description |
| ---- | ----------- |
| `Take Profit (pips)` | Target distance for single open positions. |
| `Start Volume` | Volume for the initial order in each direction. |
| `Max Volume` | Maximum allowed volume per order (0 disables the limit). |
| `Close Mode` | Choose between averaging exits or partial closes. |
| `Order Step (pips)` | Minimum adverse movement before adding a new order. |
| `Minimal Profit (pips)` | Extra profit buffer added to the averaging exit. |
| `Candle Type` | Candle series used for signal generation. |

## Notes
- The strategy uses market orders only; pending orders from the original EA are emulated by evaluating conditions on each candle.
- The implementation keeps per-order state to mimic MetaTrader’s ticket-based management, including partial closes and selective exits.
- Configure the candle type and symbol pip size to match the timeframe used in the MQL script for consistent behaviour.
