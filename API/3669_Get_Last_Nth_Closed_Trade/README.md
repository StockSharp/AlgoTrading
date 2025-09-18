# Get Last Nth Closed Trade Strategy

## Overview
The **Get Last Nth Closed Trade** strategy replicates the MetaTrader expert that scans the account history and prints the _n_-th closed trade starting from the most recent one. While the StockSharp implementation does not register new orders, it continuously listens to the strategy's own executions, maintains a rolling list of completed trades, and logs a detailed snapshot for the requested index whenever a trade is closed.

## How it works
1. Every execution received through `OnNewMyTrade` is validated against the optional security and strategy identifier filters.
2. A virtual position tracker accumulates entry trades, updates the average entry price when the position is increased, and decreases the remaining volume on partial exits.
3. Whenever the tracked position is reduced, the strategy generates a `ClosedTradeInfo` snapshot with direction, prices, timestamps, identifiers, volume, and a simple profit calculation based on the stored average entry price.
4. The collection keeps up to 100 recent closed trades. After each closure the strategy prints the configured snapshot to the log in a MetaTrader-like format.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `EnableStrategyIdFilter` | When `true`, only trades whose `StrategyId` matches `StrategyIdFilter` are processed. If the filter string is empty the current strategy identifier is used. | `false` |
| `StrategyIdFilter` | Strategy identifier to match when filtering is enabled. | `""` |
| `EnableSecurityFilter` | If enabled, trades are processed only when their security equals `Strategy.Security`. | `false` |
| `TradeIndex` | Zero-based index of the closed trade snapshot that should be logged. | `0` |

## Notes and limitations
- Stop-loss and take-profit levels are not exposed by StockSharp executions; the related fields are omitted from the report.
- Profit is calculated using a simple price difference multiplied by the closed volume. Commission and slippage adjustments must be added manually if required.
- The tracker handles scale-ins and partial exits by maintaining an averaged entry price. Sudden position reversals will first close the outstanding quantity and then start a new virtual position for the remaining volume.
- The log output mirrors MetaTrader's multiline `Comment` message so the information can be copied into external tools with minimal changes.
