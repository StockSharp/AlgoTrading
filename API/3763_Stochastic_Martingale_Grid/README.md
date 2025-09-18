# Stochastic Martingale Grid Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert advisor `rmkp_9yj4qp1gn8fucubyqnvb`. It combines a stochastic oscillator entry filter with a martingale-style averaging grid. The algorithm monitors finished candles, waits for the stochastic signal line to exit predefined overbought or oversold zones, and then opens a position in the direction of the reversal. When price moves against the trade, it adds averaging orders with doubled volume at fixed pip distances. Every leg carries its own take-profit target and trailing stop management, allowing positions to scale out independently once price recovers.

## Trading Logic
- **Signal detection:**
  - The %K and %D lines of a configurable stochastic oscillator are evaluated on completed candles.
  - A long setup triggers when, on the previous candle, %K was above %D and %D was below the `ZoneBuy` threshold.
  - A short setup triggers when, on the previous candle, %K was below %D and %D was above the `ZoneSell` threshold.
- **Initial execution:**
  - On a valid signal and while the account is flat, the strategy sends a market order with the `BaseVolume`.
  - The entry price is stored to manage trailing stops and later averaging orders.
- **Martingale averaging:**
  - While a position remains open, the algorithm watches for adverse price movement of `StepPips` against the latest filled order.
  - Each new averaging order doubles the previous leg’s volume (classic martingale progression) and is only placed if the total number of open legs is below `MaxOrders` and trading remains allowed.
- **Exit management:**
  - Every leg defines an individual take-profit level located `TakeProfitPips` away from its entry price.
  - Trailing stops activate once unrealized profit reaches `TrailingStopPips`; the trailing anchor is tightened whenever profits extend further.
  - If price retraces to the trailing level or reaches the take-profit level, the corresponding leg is closed while the rest of the cluster remains active.
  - When all legs exit, the strategy resets its internal state and waits for the next stochastic signal.

## Risk Management
- The martingale expansion is bounded by `MaxOrders` and the security volume limits.
- Volumes are normalized to the instrument’s `VolumeStep`, and minimum/maximum volume constraints are respected.
- Trailing stops help protect floating profits from full reversals.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Candle subscription used for indicator calculations. | 15-minute time frame |
| `BaseVolume` | Initial order volume placed on the first signal. | `0.1` |
| `TakeProfitPips` | Pip distance between each entry price and its take-profit target. | `50` |
| `TrailingStopPips` | Pip distance used for per-leg trailing stop activation and tracking. | `20` |
| `MaxOrders` | Maximum number of concurrent averaging legs (including the initial entry). | `7` |
| `StepPips` | Minimum adverse move, in pips, required before adding another averaging order. | `7` |
| `KPeriod` | Lookback length for the stochastic %K line. | `5` |
| `DPeriod` | Smoothing length for the stochastic %D line. | `3` |
| `Slowing` | Additional smoothing applied to the %K calculation. | `3` |
| `ZoneBuy` | Upper boundary that allows long setups when %K is above %D. | `30` |
| `ZoneSell` | Lower boundary that allows short setups when %K is below %D. | `70` |

## Notes
- The strategy uses the high-level StockSharp API with candle subscriptions and indicator bindings, keeping the implementation close to the original MetaTrader logic while leveraging StockSharp’s risk and visualization tools.
- Because averaging trades double the volume, ensure the instrument’s maximum allowed volume can accommodate the martingale ladder.
- As with any martingale system, proper capital management and additional risk constraints are highly recommended before deploying on a live account.
