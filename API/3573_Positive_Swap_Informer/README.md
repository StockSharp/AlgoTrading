# Positive Swap Informer Strategy

## Overview
The **Positive Swap Informer Strategy** is a StockSharp port of the MetaTrader script `Swap Informer` (MQL/41693). It periodically scans a configurable market watch list and logs every symbol that currently offers a positive long or short swap. The strategy is designed for traders who want to monitor carry opportunities or to identify instruments that provide positive overnight financing.

Unlike trading strategies, this component does not place orders. Its purpose is purely informational: every timer tick produces a consolidated report written to the strategy log so that the user can react manually or trigger further automated processes in their own workflow.

## Behaviour
1. At startup the strategy builds a watch list consisting of the primary `Strategy.Security` (optional) and any additional identifiers supplied via the `SymbolsList` parameter.
2. Level1 subscriptions are requested for every security on the list to encourage the connector to populate swap values inside the security metadata.
3. A timer with the configured `RefreshInterval` fires repeatedly. On each tick the strategy queries every security for swap-related fields exposed through `Security.ExtensionInfo`.
4. When a positive value is found the symbol is added to the report text in the following format:
   ```
   <SecurityId>: Swap Long (Buy) = <value>
   <SecurityId>: Swap Short (Sell) = <value>
   ```
5. If no positive swaps are available the strategy prints `Positive swap report: no symbols with a positive swap were found.`

The log output is intentionally identical to the MetaTrader script so that existing automation relying on the original text can be reused.

## Parameters
| Name | Description | Default |
|------|-------------|---------|
| `SymbolsList` | Comma-separated list of additional security identifiers that should be inspected. Supported delimiters: comma, semicolon, whitespace, new line, tab. | `""` |
| `RefreshInterval` | Timer period that controls how frequently the scan is executed. Values less than or equal to zero fall back to one second. | `00:00:10` |
| `IncludePrimarySecurity` | When `true` the `Strategy.Security` is automatically added to the scan list (if assigned). Disable when you only want the custom symbol list. | `true` |

## Data Requirements
* Swap values must be published by the connected broker or market data provider via `Security.ExtensionInfo`. The strategy searches for the keys `SwapBuy`, `SwapLong`, `SwapBuyPoints`, `SwapLongPoints`, `SwapSell`, `SwapShort`, `SwapSellPoints`, and `SwapShortPoints` (case-insensitive).
* If the provider uses different key names the strategy can be extended by adding the required aliases to the `_longSwapKeys` and `_shortSwapKeys` sets inside the code.
* No historical data is required; the strategy operates completely on metadata.

## Usage
1. Add the strategy to your StockSharp terminal or algorithm host and assign a portfolio if the platform requires it for activation.
2. (Optional) Set `SymbolsList` to the identifiers you want to monitor. Use the exact identifiers known to the `SecurityProvider`.
3. Configure `RefreshInterval` if the default 10 seconds does not fit your monitoring needs.
4. Start the strategy. After the first timer tick the log will display either the positive swap list or the absence message.
5. Keep the log window open or route the log output to a file/notification system depending on your workflow.

## Notes and Limitations
* The strategy does not normalize swap values. Providers often publish points, currency units, or percentages. Interpret the numbers according to your broker documentation.
* Some connectors only fill swap values after the first successful trade or after a specific trading session event. In such cases the report may remain empty until the provider updates `ExtensionInfo`.
* Because the component performs read-only operations, it is safe to run it alongside other trading strategies without interfering with orders or position management.
* The strategy intentionally avoids caching swap values between ticks. Every report is generated from the latest metadata snapshot so that sudden changes are not missed.

## Mapping from the MetaTrader Script
| MetaTrader Concept | StockSharp Equivalent |
|--------------------|-----------------------|
| `SymbolsTotal(true)` | `SecurityProvider` lookups combined with the optional primary security |
| `SymbolInfoDouble(..., SYMBOL_SWAP_LONG/SHORT)` | `Security.ExtensionInfo` lookup through predefined swap aliases |
| `Comment()` output | `LogInfo()` messages |
| 1 second timer (`EventSetTimer(1)`) | `Timer` controlled by `RefreshInterval` (default 10 seconds) |

## Extending the Strategy
* Add more alias strings to `_longSwapKeys` or `_shortSwapKeys` if your data source uses custom field names.
* Replace the `LogInfo` call with custom notification logic (e-mail, dashboard, etc.) when integrating into larger systems.
* The timer logic is encapsulated in `StartTimer`/`StopTimer`; override them or adjust `OnTimer` to implement throttling or batched exports.

## Testing
The strategy is side-effect free. Unit tests are not provided because the behaviour depends on the external data provider populating swap values. For manual verification run the strategy with a connector that exposes known positive swap instruments and confirm the log output matches expectations.
