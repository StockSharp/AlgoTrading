# Breakdown Pending Stop Strategy

## Overview
This strategy recreates the original MetaTrader "breakdown" expert advisor. It places stop orders around the previous day's range and continuously refreshes the orders each session. A trailing-stop engine replicates the stepped trailing logic of the source script, keeping the stops tight once a position starts moving in the profitable direction.

## How It Works
- **Daily preparation** – When a daily candle closes the strategy stores the high and low. At the start of the following session it cancels leftover orders and submits a buy stop above the previous high and a sell stop below the previous low. The `Min Distance (ticks)` parameter offsets the orders away from the raw levels to avoid noise.
- **Order refresh** – Whenever pending orders are filled or a new day begins, the remaining orders are cancelled and a fresh pair is submitted using the same previous-day levels. The behaviour mirrors the MQL expert that continually maintains stop entries on both sides of the market.
- **Risk controls** – Filled positions initialize stop-loss and take-profit targets based on tick distances. A stepped trailing rule raises/lowers the stop only after price gains at least `Trailing Stop (ticks) + Trailing Step (ticks)` from the entry, exactly like the original trailing-stop implementation.
- **Exits** – Positions close immediately when price touches the active stop or target. Manual trailing closes positions at market when the trailing level is violated, matching the MetaTrader logic that modified stops on each tick.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `Working Candles` | Time frame used for monitoring price action and managing stops (default 15-minute candles). |
| `Stop Loss (ticks)` | Initial protective stop distance converted to absolute price using the instrument tick size. Set to zero to disable. |
| `Take Profit (ticks)` | Initial take-profit distance. Set to zero to disable. |
| `Trailing Stop (ticks)` | Core trailing-stop distance. Set to zero to disable trailing. |
| `Trailing Step (ticks)` | Additional profit required before the trailing stop is moved. |
| `Min Distance (ticks)` | Offset added to the previous day's high/low when placing pending orders. |
| `Order Volume` | Quantity sent with both stop orders. |

## Usage Notes
- Configure the strategy on instruments that publish daily candles so the previous session range can be obtained.
- The logic assumes a constant tick size. For instruments with variable tick increments, adjust the defaults accordingly.
- The strategy does not implement percentage-based sizing from the original MQL script; volume is defined explicitly through the `Order Volume` parameter.
- No Python version is provided for this strategy yet.
