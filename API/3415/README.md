# Watch List Linker Lite Strategy

The **Watch List Linker Lite Strategy** is a StockSharp port of the MetaTrader 4 expert advisor "VR Watch List and Linker Lite". The original MQL script reacts to the moment when a trader drags a symbol from the Market Watch window onto any chart: it forces every other open chart to immediately switch to the dragged instrument while preserving their individual time frames. StockSharp does not expose direct control over the visual chart windows, so the port reproduces the functional core by binding multiple candle subscriptions—each one using a different time frame—to the strategy security. When the strategy starts it resolves the user-specified periods, creates the subscriptions and logs that every linked time frame is now following the same instrument.

## Behaviour

1. Parse the comma-separated `LinkedTimeFrames` parameter. Each entry may use formats such as `00:15:00`, `15m`, `M15`, `1H` or plain numbers interpreted as minutes. Invalid pieces are reported through `LogWarning`.
2. If none of the entries can be parsed, the strategy falls back to a single 1-minute chart and logs the substitution.
3. Subscribe to candles for every resolved time frame through the high-level `SubscribeCandles` API and start the subscription immediately. This mirrors how the MetaTrader EA attaches the dragged symbol to each open chart.
4. Log a confirmation for each time frame showing the final security identifier and the resolved period. The logs replace the graphical redraw performed by the MetaTrader version.

The strategy does not place orders or hold positions. It only maintains the linked candle streams and therefore integrates easily into composite dashboards where several chart panels must observe the same instrument.

## Parameters

| Name | Default | Description |
| --- | --- | --- |
| `LinkedTimeFrames` | `"00:15:00,00:30:00,01:00:00"` | Comma-separated list of candle durations that should mirror the main strategy security. Each value accepts `hh:mm:ss`, suffix (`15m`), prefix (`M15`) or numeric minute formats. |

## Implementation Notes

- The port uses the high-level candle subscription API instead of manipulating low-level chart handles. This keeps the logic aligned with the framework recommendations from `AGENTS.md`.
- Time frame parsing is handled through helper methods that understand several user-friendly notations (prefix, suffix and ISO-like `hh:mm:ss`). Unsupported tokens are ignored but logged so operators can fix typos quickly.
- `GetWorkingSecurities` yields the resolved candle types to make the designer aware of the data requirements ahead of time.
- Because StockSharp cannot change chart windows directly, the strategy logs the linkage instead. Front-end dashboards can react to the log entries or to the active subscriptions to refresh their visuals.

## Usage

1. Assign the desired `Security` before starting the strategy.
2. Leave `LinkedTimeFrames` as the default list or customise it with the required periods (for example `"M1,M5,H1"`).
3. Start the strategy. The log window should display messages confirming that every linked time frame now follows the selected security.
4. Optional: adjust the parameter and restart the strategy whenever a different collection of charts must be synchronised.

## Differences from the MetaTrader Version

- The MetaTrader EA enumerated open charts automatically. The StockSharp port relies on explicit configuration through `LinkedTimeFrames` because the chart window collection is not exposed by the framework.
- Drag-and-drop is replaced by normal strategy parameter management. The linkage occurs during `OnStarted` instead of reacting to runtime UI events.
- Visual redraw is swapped for log messages and active data subscriptions, which are the idiomatic way of driving dashboards inside StockSharp.
