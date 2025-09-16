# Binary Option Symbol Scanner Strategy

## Overview
This strategy reproduces the behaviour of the original MetaTrader expert advisor
from `Binary Option Symbol.mq4`. The goal is to inspect a list of available
securities and highlight the ones that behave like binary option symbols. In
MetaTrader this is achieved by filtering all terminal symbols and printing those
where `MODE_PROFITCALCMODE == 2` and `MODE_STOPLEVEL == 0`. The converted
StockSharp strategy follows the same concept by checking security metadata that
is provided by the connected data feed.

## Strategy logic
1. Read the `Symbols` parameter and parse the identifiers (comma, semicolon or
   whitespace separated).
2. For every identifier resolve a `Security` object through the strategy
   `SecurityProvider`.
3. Look up two metadata entries inside `Security.ExtensionInfo`:
   - `ProfitCalcMode` – expected to equal the configured `ProfitCalcMode`
     parameter (default `2`).
   - `StopLevel` – expected to equal the configured `StopLevel` parameter
     (default `0`).
4. If both conditions are satisfied, output an informational log entry marking
   the security as a binary option candidate.
5. Securities without the necessary metadata are reported with debug messages
   so you can update the data vendor or mapping.
6. When the `Symbols` list is empty, the strategy falls back to the `Security`
   property assigned in the host application and analyses only that
   instrument.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `Symbols` | Comma or semicolon separated security identifiers that should be inspected. Leave blank to analyse the strategy `Security`. | *(empty)* |
| `ProfitCalcMode` | Required profit calculation mode value. Matches the constant `MODE_PROFITCALCMODE` used in the MQL script. | `2` |
| `StopLevel` | Required stop level value. Matches `MODE_STOPLEVEL` from the MQL environment. | `0` |

## Usage notes
- Ensure that the data adapter populates the `ExtensionInfo` dictionary with
  fields named `ProfitCalcMode` and `StopLevel`. Without these keys the
  strategy cannot evaluate the filters.
- When a symbol cannot be resolved through `SecurityProvider.LookupById` the
  strategy will emit a warning log. Update the identifier or add the security
  to your connection before running the scan.
- All detections are written via `AddInfoLog`. Enable the info log level in the
  runner to capture the output.
- The strategy does not submit any orders. It is purely diagnostic and can be
  safely executed in research or production connections.

## Differences vs. original MQL implementation
- MetaTrader automatically iterated over `SymbolsTotal`. StockSharp does not
  expose a direct equivalent, therefore the `Symbols` parameter explicitly
  defines the universe to scan. The fallback to the strategy `Security` keeps
  the default behaviour lightweight.
- Instead of `MarketInfo`, the conversion relies on `Security.ExtensionInfo`
  metadata. This gives the same flexibility while staying within the
  high-level StockSharp API guidelines.
- Extensive logging was added to make it clear why a symbol was or was not
  marked as a binary option candidate.

## Quick start example
1. Connect to your trading venue using the StockSharp terminal or a custom
   host application.
2. Create a new instance of `BinaryOptionSymbolScannerStrategy`.
3. Set `Symbols` to something like `EURUSD, NAS100, XAUUSD, EURGBP`.
4. Start the strategy. The log will contain entries such as:
   `Binary option symbol detected: EURUSD`. Update the list based on the
   results.

This detailed report can then be used to configure other automated strategies
or to build watch lists dedicated to binary option trading.
