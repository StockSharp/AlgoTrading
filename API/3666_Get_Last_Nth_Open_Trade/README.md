# Get Last Nth Open Trade Strategy

This utility strategy replicates the original MetaTrader expert advisor that scans all open trades and prints the *N*-th most recent one. It runs on a timer and inspects the portfolio positions without sending any orders. The snapshot of the selected trade is logged through the strategy log so it can be reviewed inside StockSharp Designer.

## How it works

1. Once started the strategy validates that a portfolio is assigned and a positive refresh interval is configured.
2. A timer triggers according to the `RefreshInterval` parameter. Each tick rebuilds a list of currently open positions (non-zero quantity).
3. Optional filters remove positions that do not match the configured security (`EnableSymbolFilter`) or the strategy identifier (`EnableMagicNumber` and `MagicNumber`).
4. The remaining positions are ordered by their `LastChangeTime` (newest first). The strategy then picks the entry specified by `TradeIndex` (zero-based) and builds a detailed snapshot.
5. The snapshot contains ticket, symbol, volume, average price, stop-loss, take-profit, profit, comment (when available), side, and open/close timestamps. Missing fields are logged as empty strings.
6. Only changes are logged: if the new snapshot matches the previous output nothing new is written, which keeps the log tidy.

## Parameters

| Parameter | Description |
|-----------|-------------|
| `EnableMagicNumber` | When enabled the strategy uses the `MagicNumber` value to filter positions by their `StrategyId`. Non-numeric identifiers are ignored. |
| `EnableSymbolFilter` | Limits the scan to positions that belong to the strategy security. |
| `MagicNumber` | Numeric identifier (strategy id) required when `EnableMagicNumber` is true. |
| `TradeIndex` | Zero-based index of the position to report. `0` refers to the most recent entry after sorting. |
| `RefreshInterval` | Delay between scans. The default `1` second mimics the tick-driven behaviour of the original EA. |

## Notes

- The strategy does not place orders; it is a monitoring tool intended for dashboards or automated supervision.
- If no positions match the configured filters a descriptive message is logged instead of the trade snapshot.
- `LastTradeSnapshot` property exposes the most recent message so other components can read it programmatically.
- Stop-loss, take-profit, comment, and timestamps depend on the data provided by the broker. When unavailable the corresponding fields appear blank.

## Migration differences vs. MQL version

- StockSharp aggregates orders into `Position` objects, therefore the strategy reads from the portfolio instead of raw trade tickets.
- MetaTrader sorts by ticket identifiers; StockSharp sorts by the last change timestamp and then by identifier to achieve similar behaviour.
- The MetaTrader `Comment` field is retrieved via reflection because it is optional in StockSharp. If the property does not exist it is simply omitted from the snapshot.
- Screen comments from the MQL EA are replaced with log messages inside the strategy to integrate better with the Designer environment.
