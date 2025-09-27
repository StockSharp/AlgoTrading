# Trailing Stop Trigger Manager Strategy

## Overview
The **Trailing Stop Trigger Manager Strategy** is a StockSharp port of the MetaTrader expert advisor `Trailing Sl.mq5`. The original EA
did not open trades on its own. Instead, it monitored already open positions with a matching *magic number* and tightened their
stop-loss levels when the market moved in the desired direction. This C# implementation reproduces that behaviour using
StockSharp's high-level strategy API, delivering transparent trailing-stop management that works with any instrument supported by
StockSharp.

## Trailing logic
1. Subscribes to the order book in order to read the latest best bid and best ask quotes.
2. Detects whether the strategy currently holds a long or short net position.
3. Calculates the floating profit using the appropriate side of the market (best bid for longs, best ask for shorts).
4. Activates the trailing mode once the profit exceeds `TriggerPoints` (converted to price units through `PriceStep`).
5. Sets the trailing stop at the configured distance `TrailingPoints` away from the current market quote.
6. Moves the trailing stop only towards the market to keep locking in additional profit.
7. Sends a market order to flatten the position as soon as the best quote touches the calculated trailing stop level.

## Order and risk management
- The strategy does **not** submit initial entry orders. It only manages an existing position that may have been opened manually
  or by another strategy.
- Market exits are placed with `BuyMarket`/`SellMarket`, mirroring the `PositionModify` calls from the original MetaTrader code.
- The stop distance automatically scales with the instrument's `PriceStep`, which preserves the point-based configuration from
  the EA.
- Once the position is closed, the trailing state resets so that new positions start from a clean slate.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TrailingPoints` | `int` | `1000` | Distance between the current price and the trailing stop, measured in price steps. |
| `TriggerPoints` | `int` | `1500` | Minimum profit in price steps required to start trailing the position. |

## Usage notes
- Attach the strategy to the security whose position you want to supervise. It will immediately start tracking the existing
  exposure.
- Configure the initial `Volume` of the strategy to match the size of your open position. StockSharp uses net positions, so the
  strategy will exit the entire lot when the trailing stop is triggered.
- If the broker delivers coarse price steps, adjust `TrailingPoints` and `TriggerPoints` accordingly to avoid premature exits.
- The strategy keeps its state entirely inside StockSharp, so it can be combined with any discretionary or automated system that
  leaves the actual order execution to StockSharp.

## Differences from the original MetaTrader expert
- MetaTrader managed separate positions per ticket and filtered them by *magic number*. StockSharp works with a net position per
  security, removing the need for ticket filtering.
- The `Setloss`, `TakeProfit`, and `Lots` inputs were unused in the original EA. They are therefore omitted in the StockSharp
  version to keep the configuration focused on trailing behaviour.
- Order modifications are replaced by direct market exits, which is the idiomatic approach for netting accounts in StockSharp.
