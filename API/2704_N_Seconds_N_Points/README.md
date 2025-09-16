# N Seconds N Points Strategy

## Overview
This strategy reproduces the money-management behaviour from the original MetaTrader script "N seconds N points". It does not generate trade entries on its own. Instead, it monitors the net position of the assigned security and manages take-profit exits after a configurable waiting period. The approach is designed for discretionary or automated setups where entries are produced elsewhere and need a consistent, time-based profit management layer.

## Core Logic
1. The strategy records every fill to maintain the average entry price, the current net volume and the time of the active position.
2. Real-time trades are subscribed in order to keep the latest market price. A one-second timer emulates the original EA timer and performs periodic checks.
3. Once a position has been open for at least `WaitSeconds`, the strategy evaluates the unrealised profit:
   - If the price has moved by at least `TakeProfitPips` (converted into points/pips based on the instrument precision), the position is closed immediately with a market order.
   - Otherwise, the strategy places or refreshes a take-profit limit order at the corresponding price level. The order is only sent when the distance to the limit is greater than one pip to avoid placing the target too close to the current price.
4. When the position is flattened, every protective order is cancelled and the internal state is reset so the next position starts with a clean slate.

## Parameters
- **WaitSeconds** – number of seconds the position must remain open before the profit rules become active. The default value is 40 seconds.
- **TakeProfitPips** – distance in pips that defines the profit target. The strategy automatically adapts the pip value to 3/5-digit forex symbols and uses the instrument price step for other markets. The default value is 15 pips.

## Implementation Details
- The strategy uses `SubscribeTrades()` to capture the latest executed price, ensuring that profit calculations reflect live market data.
- `Timer.Start(TimeSpan.FromSeconds(1), ProcessTimer)` provides a one-second heartbeat, closely matching the MetaTrader timer granularity while allowing precise elapsed-time tracking.
- Take-profit orders are normalised to the instrument price step to avoid rejected orders due to invalid price increments.
- Average entry price and entry time are recomputed after partial fills, additions or reversals so that the time filter always references the effective position.

## Practical Notes
- Because the strategy only manages exits, pair it with another strategy, manual trading or external signals to generate entries.
- Ensure the account supports limit orders at the calculated step size; otherwise reduce the pip distance or adjust the instrument settings.
- The timer keeps running even when the market is closed. If the data feed pauses, the most recent known price is used until a new trade arrives.
