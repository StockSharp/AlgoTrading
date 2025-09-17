# Market Watch Panel Strategy

## Overview
The **Market Watch Panel Strategy** is a StockSharp port of the MetaTrader script *Market Watch Panel.mq5*. The original script builds a graphical panel that allows the trader to compile a watch list of symbols, persist the list to a text file, and continuously refresh the displayed prices. This C# implementation focuses on the underlying mechanics of the panel:

- Loading a list of symbols from a `symbols.txt` file.
- Subscribing to real-time Level1 data for every tracked instrument.
- Logging the latest price for each symbol whenever a Level1 update arrives.
- Offering programmatic helpers that mimic the panel buttons to add, reload, or clear the watch list.

The strategy never submits orders. It is designed to act as an always-on market monitor that feeds the terminal log with the most recent quotes for the configured instruments.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `SymbolsFileName` | Relative or absolute path to the text file containing one symbol identifier per line. The strategy creates the file when it does not exist. |
| `IncludePrimarySecurity` | When enabled (default), the main strategy security is always observed, but it is not written to the symbols file. |

Both parameters are declared as non-optimizable `StrategyParam<T>` objects so that they can be managed from the StockSharp UI just like the original color and layout inputs in MetaTrader.

## Runtime Controls
The MetaTrader panel exposed two buttons and a text input. The strategy mirrors these controls with three public methods:

- `AddSymbol(string symbolCode)` – adds the specified instrument to the watch list, subscribes to its Level1 feed, and writes the updated list back to `symbols.txt`.
- `ReloadSymbols()` – flushes every persistent entry, reloads the `symbols.txt` file, and restores subscriptions. Use it when the text file is edited outside of the strategy.
- `ClearSymbols()` – removes all persistent entries from the watch list, unsubscribes from their Level1 feeds, and truncates the `symbols.txt` file. The primary security (if enabled) remains active to match the original panel behaviour.

Every Level1 subscription is stopped automatically when the strategy stops or resets, preventing background data streams from lingering.

## Data Flow
1. **Startup** – `OnStarted` clears previous state, loads `symbols.txt`, and creates Level1 subscriptions for each symbol in the file.
2. **Primary Security** – when `IncludePrimarySecurity` is `true`, the strategy adds the main security after loading the file but does not persist it.
3. **Live Updates** – every Level1 change triggers the log entry `Market Watch update: <symbol> price=<value>`, mimicking the dynamic label refresh from MetaTrader.
4. **Persistence** – the helper methods update `symbols.txt` immediately, keeping the file in sync with the active watch list.

## File Format
The watch list file is a plain UTF-8 text file that contains one symbol identifier per line. Blank lines and duplicates are ignored automatically. The file lives in the strategy working directory by default, but any absolute path can be used.

## Usage Steps
1. Place the compiled strategy assembly into your StockSharp application and assign the desired portfolio/security pair.
2. Configure `SymbolsFileName` if a custom location is required.
3. Start the strategy. It will create or load `symbols.txt` and begin logging price updates.
4. Call `AddSymbol`, `ReloadSymbols`, or `ClearSymbols` from the scripting console or from custom UI actions to manage the watch list during runtime.
5. Review the log to monitor the latest bid/ask/last price information for every tracked symbol.

## Differences from the MetaTrader Version
- No graphical user interface is provided. Interaction is done via strategy parameters and public methods.
- The StockSharp version focuses solely on Level1 subscriptions and logging; it does not display or store prices in custom labels.
- Color settings from the original script are intentionally omitted because StockSharp handles visualization differently.

Despite these adjustments, the core workflow—managing a persistent list of symbols and updating their current prices—remains faithful to the MetaTrader logic.
