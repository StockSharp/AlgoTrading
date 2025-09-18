# CloseAllControlStrategy

## Overview

The **CloseAllControlStrategy** is a StockSharp conversion of the MetaTrader "CloseAll" utility from `MQL/38245/CloseAll.mq4`. The strategy executes a one-shot bulk management command as soon as it starts, allowing you to close existing positions and cancel pending orders that match flexible filters. It is tailored for multi-symbol portfolios where traders need to quickly flatten exposure or remove specific orders based on comment tags, magic numbers, or symbol identifiers.

Unlike signal-driven strategies, this utility does not subscribe to market data or wait for ticks. It simply inspects the current portfolio state, performs the selected close action, and immediately stops itself. That makes it a handy panic button or end-of-session tool inside StockSharp Designer and AlgoTrading environments.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `OrderComment` | `string` | `"Bonnitta EA"` | Substring (case-insensitive) that must be present inside an order or position comment to be processed. Leave empty to ignore the comment filter. |
| `Mode` | `CloseAllMode` | `CloseAll` | Selects which bulk close scenario is executed. See the mode matrix below for full behaviour. |
| `CurrencyId` | `string` | empty | Security identifier used by currency-aware modes. When empty the strategy falls back to the strategy `Security`. |
| `MagicOrTicket` | `long` | `1` | Identifier compared against order tickets and strategy ids. Positive values are required to activate magic/ticket filters. |

### Mode matrix

| Mode | Positions | Pending orders | Additional filters |
|------|-----------|----------------|--------------------|
| `CloseAll` | Close every matched long and short position. | — | Comment filter only. |
| `CloseBuy` | Close only long positions. | — | Comment filter only. |
| `CloseSell` | Close only short positions. | — | Comment filter only. |
| `CloseCurrency` | Close positions whose security matches `CurrencyId` (or the current strategy security when empty). | — | Comment + currency filters. |
| `CloseMagic` | Close positions whose identifiers match `MagicOrTicket`. | — | Comment + magic filters. |
| `CloseTicket` | Close the single position whose ticket matches `MagicOrTicket`. | — | Comment + ticket filter. |
| `ClosePendingByMagic` | — | Cancel pending orders whose identifiers match `MagicOrTicket`. | Comment + magic filters. |
| `ClosePendingByMagicCurrency` | — | Cancel pending orders that satisfy both the magic number and currency filter. | Comment + magic + currency. |
| `CloseAllAndPendingByMagic` | Close positions and cancel pending orders that match the magic number. | Cancel | Comment + magic filters. |
| `ClosePending` | — | Cancel every pending order that passes the comment filter. | Comment filter only. |
| `CloseAllAndPending` | Close all positions. | Cancel all pending orders. | Comment filter only. |

### Identifier matching

* `MagicOrTicket` is compared against multiple fields to maximise compatibility: `Order.TransactionId`, `Order.Id`, `Order.UserOrderId`, and (when numeric) `StrategyId` for orders and positions.
* The comparison is numeric first (culture-invariant). If numeric parsing fails the strategy performs a string comparison against the decimal representation of `MagicOrTicket`.
* Values ≤ 0 disable the magic/ticket filters.

### Comment handling

* The comment filter trims both the configured filter and the inspected comment before comparison.
* Comparison is case-insensitive and checks for the filter as a substring (equivalent to the original `StringFind` behaviour).
* Positions inherit their comment through reflection. When a comment is not available the filter rejects the position, mirroring MetaTrader behaviour when the comment does not match.

## Execution flow

1. Validate that a portfolio has been assigned and start protection once.
2. Select the active mode and collect the positions/pending orders that satisfy all filters.
3. Submit the required market or cancel orders.
4. Stop the strategy immediately after issuing the bulk action.

The strategy never queues duplicate protection orders and does not subscribe to any data feeds. All operations are executed using the high-level `BuyMarket`, `SellMarket`, and `CancelOrder` helpers.

## Usage tips

* Assign the desired portfolio and (optionally) strategy security before clicking **Start**.
* Adjust `OrderComment` to match the EA tag or text you want to target.
* Use the `CurrencyId` parameter when you need to restrict closing to a specific instrument code (for example `EURUSD@FORTS`).
* Set `MagicOrTicket` to the exact numeric identifier used by your automation. Leave it at the default `1` only if that value is relevant; set it to `0` to disable magic-based filters.
* Because the strategy stops itself after execution you can keep it inside a Designer diagram and trigger it on demand with minimal risk of repeated closures.

## Differences from the original MQL script

* Graphic buttons (`Close Orders` and `Exit`) are replaced with immediate execution when the strategy starts.
* Pending order detection uses `OrderTypes` instead of explicit `OP_*` constants and covers all non-market order types supported by StockSharp.
* The strategy attempts to read position comments and strategy identifiers via reflection to stay compatible with broker adapters that expose them.
* Logging is handled by the base `Strategy` infrastructure (no explicit prints). You can add custom logging if required.
