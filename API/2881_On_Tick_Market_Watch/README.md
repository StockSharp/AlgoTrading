# On Tick Market Watch Strategy

## Overview
The **On Tick Market Watch Strategy** replicates the behaviour of the MetaTrader script `scOnTickMarketWatch.mq5`. The original script continuously scans the Market Watch list and raises a custom event whenever a new tick arrives for any symbol, printing the bid price and spread information. This C# port converts that behaviour into a StockSharp high-level strategy that listens for Level1 updates and logs the tick information through the strategy logger.

The strategy is intentionally non-trading. Its purpose is to provide diagnostics or monitoring of incoming tick data across multiple instruments connected to the same connector. Because it relies on StockSharp's data subscriptions, the solution is event-driven and does not require manual delays or loops like the MQL version.

## Key Features
- Monitors the primary strategy security and any additional securities defined in a comma-separated list.
- Subscribes to Level1 data for each security in order to capture bid/ask updates.
- Calculates the spread (ask minus bid) whenever both sides are available and logs detailed information in English.
- Mirrors the Market Watch index by keeping an internal ordering identical to the user-specified list.
- Provides friendly warnings when a symbol cannot be resolved by the configured `SecurityProvider`.

## Parameters
| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `SymbolsList` | `string` | `""` | Comma-separated list of extra security identifiers (e.g. `AAPL@NASDAQ,MSFT@NASDAQ`) that should be observed in addition to the main `Strategy.Security`. Each identifier must exist in the current `SecurityProvider`. |

## How It Works
1. During `OnStarted`, the strategy resolves all symbols. The main `Strategy.Security` is always added first, followed by any extra symbols supplied through `SymbolsList`.
2. For every resolved security, the strategy calls `SubscribeLevel1` and attaches a callback that receives `Level1ChangeMessage` updates.
3. Each callback verifies that the update contains at least one of the relevant price fields (`LastTradePrice`, `BestBidPrice`, or `BestAskPrice`).
4. The bid is taken from `BestBidPrice` (or falls back to `LastTradePrice` if the best bid is missing), the ask comes from `BestAskPrice`, and the spread is computed if both values are present.
5. The logger prints a message matching the original script: `New tick on the symbol <id> index in the list=<index> bid=<bid> spread=<spread>`. When the ask is unavailable, `spread` is reported as `n/a`.
6. If StockSharp cannot find a requested symbol in the `SecurityProvider`, a warning message is emitted and the symbol is skipped.

## Usage Instructions
1. Assign the main security (`Strategy.Security`) through the strategy configuration UI or in code.
2. Optionally set the `SymbolsList` parameter with additional comma-separated identifiers. The order determines the reported index in the log output.
3. Connect the strategy to a data source capable of delivering Level1 information for the chosen instruments.
4. Start the strategy. It will immediately subscribe to Level1 data and begin logging tick messages.
5. Review the strategy log to verify incoming market data and calculated spreads.

## Notes and Differences vs. MQL Version
- The StockSharp version is fully event-driven. There is no manual loop or `Sleep` call; the platform invokes callbacks when data arrives.
- `SymbolsTotal(true)` from MQL is emulated by preserving the order in which securities are added to the watch list. The reported index starts at zero for the main strategy security.
- Spread values in MetaTrader are point-based integers. In StockSharp the spread is calculated as a decimal price difference.
- Custom chart events are replaced with log entries because StockSharp strategies already include a flexible logging subsystem.
- If a symbol lacks an ask price in the current update, the spread is reported as `n/a`, providing clarity on incomplete Level1 information.
- The strategy is designed strictly for monitoring and does not send any orders.

## Example Log Output
```
New tick on the symbol AAPL@NASDAQ index in the list=0 bid=171.25 spread=0.02
New tick on the symbol MSFT@NASDAQ index in the list=1 bid=324.10 spread=n/a
```
These entries demonstrate how the bid and spread information is reported for each tracked instrument in the Market Watch list.
