# OnTick Multisymbol Strategy

## Overview
- Recreates the MetaTrader 5 template **OnTick(string symbol).mq5**, which routes every tick of the configured instruments into a single callback.
- Subscribes to Level-I trade data for each resolved symbol and logs a message whenever a tick is received.
- Provides StockSharp-style parameterization so the list of monitored instruments can be modified from the UI or optimization environment.

## Original Expert Features
- The MQL5 template exposes an `OnTick(string symbol)` handler that prints the name of the symbol producing the event.
- A compile-time list of instruments (`SYMBOLS_TRADING`) or the special `MARKET_WATCH` token controls which symbols generate notifications.
- Initialization, chart events, and deinitialization handlers are intentionally empty.

## StockSharp Adaptation
- The strategy exposes a **Symbols** parameter (comma-separated list) that matches the `SYMBOLS_TRADING` preprocessor directive from the original template.
- Every resolved `Security` is subscribed via `SubscribeTrades(security)`; received ticks are logged with timestamp, price, and volume, reproducing the informational `Print()` call from MQL5.
- If `SecurityProvider` cannot resolve a symbol, the strategy issues a warning and continues with the remaining instruments.
- The special `MARKET_WATCH` keyword is not resolved automatically; a warning is produced so the user can provide explicit identifiers compatible with the current data source.

## Parameters
| Name | Description | Notes |
| --- | --- | --- |
| `Symbols` | Comma/semicolon/space-separated list of instrument identifiers to monitor for tick events. | Defaults to `EURUSD,GBPUSD,USDJPY,USDCHF`. The primary `Security` assigned to the strategy is always monitored even if it is not present in the list. |

## Data Subscriptions
- `SubscribeTrades(security)` – captures tick trades for each configured symbol, matching the tick-driven behavior of the template.
- `GetWorkingSecurities()` declares the tick data requirement for every resolved symbol so designers and optimizers allocate the correct feeds.

## Usage Notes
1. Attach the strategy to a connector with an initialized `SecurityProvider`.
2. Optionally assign the `Security` property to monitor an additional primary instrument.
3. Edit the **Symbols** parameter to add/remove comma-separated identifiers that exist in the connected marketplace.
4. Start the strategy: every incoming tick produces a log entry such as `Tick received for EURUSD@2024-02-01T10:00:00Z: price=1.08450, volume=1`.
5. Review the log stream or attach custom logic to the `OnTrade` method if further processing is needed.

## Differences vs. MQL Version
- Uses runtime parameters instead of preprocessor macros, allowing UI-based configuration and optimization.
- Warnings replace silent failures when an identifier is missing from the data source.
- Does not auto-discover `MARKET_WATCH`; users should provide explicit codes or extend the strategy to query the connected provider.
