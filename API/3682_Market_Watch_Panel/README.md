# Market Watch Panel Strategy

## Overview
The **MarketWatchPanelStrategy** replicates the MetaTrader 5 "Market Watch Panel" expert by turning it into a StockSharp high-level strategy. Instead of drawing a custom GUI panel, the strategy loads a configurable list of symbols from disk, subscribes to real-time Level 1 streams, and reports price updates through the strategy log. Optional price logging can be enabled to persist snapshots to a text file for post-trade analysis or dashboard integration.

## Key Behaviors
1. **Symbol management through text files**
   - The watch list is stored in `symbols.txt` by default (one symbol per line).
   - On start the strategy reads the file, removes duplicates, and subscribes to every valid instrument.
   - Public helper methods allow adding new symbols or clearing the watch list directly from the Designer without editing files manually.
2. **Live price monitoring**
   - Each subscribed security routes Level 1 messages to the `ProcessLevel1` handler.
   - The handler prints human-readable messages (`"SYMBOL last price: VALUE"`) whenever new trade or close prices arrive.
   - The latest price per security is cached inside the strategy so the most recent snapshot is always available for further processing.
3. **Optional price logging**
   - When the `EnablePriceLogging` parameter is set to `true`, every change in the last price is appended to `symbols_prices.log` (UTC timestamp; instrument identifier; price).
   - Duplicate prices are ignored to keep the log concise.
   - Errors during file writes are automatically reported through `LogError`.
4. **Runtime updates**
   - Calling `AddSymbol("TICKER")` appends the ticker to the file, starts a new subscription instantly if the strategy is running, and prevents duplicates via case-insensitive checks.
   - Calling `ClearSymbols()` disposes existing subscriptions, empties the in-memory cache, and rewrites the symbols file so it contains no instruments.

## Strategy Parameters
| Name | Description | Notes |
|------|-------------|-------|
| `SymbolsFile` | Path to the plain-text file that stores the watch list (one symbol per line). | Defaults to `symbols.txt`. The strategy warns if the file does not exist and waits for new symbols. |
| `PriceLogFile` | Destination file for price snapshots when logging is enabled. | Defaults to `symbols_prices.log`. Uses append mode so historical records remain intact. |
| `EnablePriceLogging` | Enables writing price updates to `PriceLogFile`. | Disabled by default to avoid disk I/O in latency-sensitive scenarios. |

## Conversion Notes
- The original MetaTrader script rendered GUI elements, read/write `symbols.txt`, and refreshed label captions every tick. StockSharp strategies do not manipulate MT5 dialog controls, so logging replaces the visual panel.
- Button actions from the MQL5 panel are reproduced as public methods: `AddSymbol` mirrors the "Add" button and `ClearSymbols` mirrors the "Reset" button.
- `LoadSymbolsFromFile` and `SaveSymbolsToFile` keep the same semantics as in MQL5—creating/rewriting the text file as needed.
- Level 1 subscriptions (`SubscribeLevel1`) provide the same immediate price updates that the MT5 panel polled via `iClose` calls inside `OnTick`.

## Usage
1. Create or edit the text file referenced by `SymbolsFile` (default `symbols.txt`) and place one instrument identifier per line.
2. Launch the strategy in StockSharp Designer or programmatically through a connector.
3. Observe the strategy log for live price updates. Enable `EnablePriceLogging` if you want to archive updates to disk.
4. To add a new instrument at runtime call `AddSymbol("NEW_SYMBOL")`. To wipe the watch list call `ClearSymbols()`.

## Files
```
3682_Market_Watch_Panel/
├── CS/
│   └── MarketWatchPanelStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```
