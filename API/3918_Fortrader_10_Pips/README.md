# Fortrader 10 Pips Strategy

## Overview
The **Fortrader 10 Pips Strategy** is a StockSharp port of the MetaTrader 4 expert advisor `10pips.mq4` (strategy ID 8074). The robot simultaneously keeps one long and one short position open. Each leg uses fixed take-profit, stop-loss and trailing-stop distances measured in symbol points.

This conversion recreates the hedging behaviour within StockSharp's high-level API. Immediately after the strategy starts it sends a market buy and a market sell order. Whenever a protective order closes a leg the strategy instantly opens a new order in the same direction, keeping two opposing positions alive at all times.

## Parameters
| Name | Description |
| --- | --- |
| `Take Profit Buy` | Take-profit distance for the long leg, in points. |
| `Stop Loss Buy` | Stop-loss distance for the long leg, in points. |
| `Trailing Stop Buy` | Trailing-stop distance for the long leg, in points. Set to zero to disable trailing. |
| `Take Profit Sell` | Take-profit distance for the short leg, in points. |
| `Stop Loss Sell` | Stop-loss distance for the short leg, in points. |
| `Trailing Stop Sell` | Trailing-stop distance for the short leg, in points. Set to zero to disable trailing. |
| `Volume` | Volume of every market order in lots. |

All distances are multiplied by the instrument's `PriceStep` to convert from points to absolute price values. Each parameter is exposed through `StrategyParam<T>` so the strategy can be tuned or optimised via the GUI.

## Trading Logic
1. **Startup** – `OnStarted` subscribes to Level 1 data to track the current best bid and ask prices. The strategy immediately sends one market buy and one market sell order.
2. **Protective orders** – After each entry fill (`OnNewMyTrade`) the strategy creates the associated stop-loss and take-profit orders if the distances are greater than zero. Orders are rounded to the nearest price step.
3. **Re-entry** – When a stop-loss or take-profit order executes, the closed leg is reopened instantly with a new market order so the two-directional exposure persists.
4. **Trailing stops** – Level 1 updates trigger `UpdateTrailingStops`, which adjusts the stop-loss orders whenever the current bid/ask has moved beyond the configured trailing distance from the entry price. The logic mirrors the original EA: trailing starts once the profit exceeds the trailing distance, and stops are moved only in the direction of profit.

## Implementation Notes
- The original MT4 code waited 10 seconds between the initial buy and sell orders. StockSharp does not require this delay, therefore both orders are sent right away.
- Because StockSharp uses net positions by default, true hedging may depend on the broker/connector supporting opposing positions. The strategy keeps track of each leg independently and re-establishes them after every exit.
- `StartProtection()` is called once during `OnStarted` so that global risk protections are active if configured in the framework settings.

## Usage Tips
- Ensure the selected connector supports simultaneous long and short positions if the hedging behaviour is required.
- Set trailing distances to zero to disable trailing for the corresponding leg.
- Optimise the risk parameters (`Take Profit`, `Stop Loss`, `Trailing Stop`) on historical data to fit the traded symbol and timeframe.
