# Dual Stoploss Strategy

This strategy replicates the behaviour of the MetaTrader expert **Dual StopLoss.mq4**. It acts as a risk-management layer: it monitors the protective stop-loss orders attached to open positions and closes those positions a few points before the stop would trigger. The early exit is designed to avoid negative slippage on highly volatile moves while still respecting the trader's initial stop placement.

## How it works

1. The strategy subscribes to Level1 data to track the current best bid/ask and the `StopLevel` (or equivalent) distance published by the broker.
2. Every time new prices arrive or orders/trades change, it searches for the nearest active stop order that belongs to the managed security.
3. The distance between the market price and that protective stop is compared with a configurable threshold:
   - Threshold = `WhenToClosePoints × pointValue + stopLevelDistance`.
   - `pointValue` matches MetaTrader's `Point` (0.0001 for most FX pairs, auto-detected from the security settings).
   - `stopLevelDistance` comes from Level1 fields (`StopLevel`, `MinStopPrice`, `StopPrice`, or `StopDistance`) when available, otherwise zero.
4. When the remaining distance is smaller than or equal to the threshold, the position is closed immediately using a market order.

The logic covers both long and short positions. For long positions the best bid is compared with the sell stop price; for short positions the best ask is compared with the buy stop price. Only stop and stop-limit orders in the active state are considered.

## Parameters

| Parameter | Description |
|-----------|-------------|
| **WhenToClosePoints** | Distance (in MetaTrader points) from the stop level that should trigger the early exit. Default: 10. Set to zero to only rely on the broker's minimal stop level distance. |

## Notes and limitations

- The strategy does **not** open positions on its own; it only manages positions that already exist and have protective stop orders.
- Ensure that the underlying connector/broker supplies stop level values through Level1 data if you want to account for broker-imposed minimal distances. If that information is missing, the strategy still works using only the configured point distance.
- The `StartProtection()` call enables StockSharp's built-in safety guards so that emergency exits remain active once the strategy has started.
- Stops are detected from the strategy's `Orders` collection. Make sure that protective stops are registered through the same strategy instance so that they appear in this list.
- When multiple stop orders exist for the same direction, the one closest to the market is used.

## Usage tips

1. Attach the strategy to a portfolio/security where positions are opened manually or by another system, but protective stops are placed under the same strategy context.
2. Configure `WhenToClosePoints` to match how much cushion you need before the stop. This value is interpreted exactly like in MetaTrader (points, not price units).
3. Start the strategy and monitor the log. When the market price approaches the stop, the strategy will issue a market order to close the position proactively.
4. Combine this module with other entry or position-sizing strategies to create a complete trading workflow.
