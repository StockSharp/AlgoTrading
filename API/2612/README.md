# All Information About The Symbol Strategy

## Overview
The **All Information About The Symbol Strategy** is a utility strategy that replicates the inspection panel from the original MQL5 script *All information about the symbol.mq5*. Instead of drawing a UI control, the StockSharp conversion writes a comprehensive snapshot of the selected security into the strategy log as soon as the strategy starts. No trading logic or orders are generated – the entire purpose is to audit the metadata and the latest market values known to the connector.

## How It Works
1. When the strategy is started it validates that a `Security` has been assigned and prints a header.
2. The class queries the security via reflection and grouped helper methods to expose:
   - **Overview** – identification fields (code, name, classification, currency, decimals, sector, industry, margin permissions, etc.).
   - **Trading parameters** – lot size, volume/price steps, min/max price limits, different margin requirements, strike or option type if present, multiplier, settlement date, and more.
   - **Market snapshot** – best bid/ask, last trade, OHLC, settlement price, open interest, price limits, state/status, and timestamps.
   - **Security extension information** – optional dictionary dump for any custom metadata supplied by the connector.
   - **Full property dump** – an exhaustive list of every public security property (except the ones already grouped) obtained via reflection so that no attribute is missed.
   - **Exchange board details** – optional reflection dump for the related `ExchangeBoard`, covering trading permissions, available order types, trading schedule metadata, etc.
3. Each value is formatted for readability. Dates are printed using ISO-8601 format, decimals use invariant culture, dictionaries and enumerables are expanded, and references to other securities or boards are reduced to their identifiers.
4. After all sections are logged the strategy keeps running idle, allowing the operator to review the information directly in the strategy log window.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `LogBoardDetails` | `true` | Controls whether the strategy prints an extended section with properties of the associated `ExchangeBoard`. |
| `LogExtensionInfo` | `true` | Dumps the `ExtensionInfo` dictionary if the connector provides additional metadata for the security. |
| `LogFullPropertyDump` | `true` | Adds a reflection-based list of every remaining public property to ensure nothing is hidden. Disable it to reduce log size. |

## Output Structure
The log always respects the following order:
1. Start marker with the security identifier.
2. **Overview** section (individual fields printed line by line).
3. **Trading parameters**.
4. **Market snapshot**.
5. Optional **Security extension info** (skipped when empty or unavailable).
6. Optional **Full property dump** (skips already printed fields to avoid duplicates).
7. Optional **Exchange board details**.
8. Final completion marker.

This deterministic ordering makes it easy to diff two runs or to send the log to support for diagnostics.

## Usage
1. Add the strategy to a StockSharp project and assign the desired `Security` and `Portfolio` in the UI or via code.
2. Configure the three boolean parameters if you want to reduce or expand the output.
3. Start the strategy – the first log entries contain the full report. No further interaction is required.
4. Stop the strategy when you no longer need the log snapshot.

## Notes and Limitations
- The strategy never places orders or modifies positions; it is safe to run on live or backtesting connections for inspection purposes.
- Because reflection is used to enumerate properties, the full dump section may grow as StockSharp adds new fields in future versions.
- Some connectors expose additional per-venue fields through `ExtensionInfo`. If the dictionary contains complex objects they are expanded recursively, so the output can become lengthy.
- Market snapshot values reflect the information currently cached in the `Security` object. If no market data has been received yet, many fields will appear as `<null>`.

## Conversion Notes
- The original MQL5 script displayed symbol information inside a modal dialog with a list view. StockSharp strategies do not implement chart dialogs, therefore the conversion focuses on logging the same data categories.
- Support functions from the script (`IsExpirationTypeAllowed`, `IsFillingTypeAllowed`) are represented implicitly by dumping the board settings that define supported order expiration and filling modes.
- Additional commentary lines replace the MQL `Comment` output with standard StockSharp `LogInfo` entries for consistency with existing samples.
