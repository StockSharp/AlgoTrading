# SaveTicks Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Captures bid/ask snapshots for one or more securities on a fixed schedule and stores them in CSV and binary files. Designed for building historical tick datasets rather than placing orders.

## Details

- **Purpose**: Periodically record the best bid, best ask and last trade price for the selected securities.
- **Data Sources**: Uses Level1 subscriptions for every tracked security to obtain quote and trade updates.
- **Scheduling**: A background timer fires every `Recording Interval` and writes the latest snapshot to disk.
- **Outputs**:
  - Individual CSV files for each security with timestamp, bid, ask and last columns.
  - Optional binary files storing the same fields together with presence flags.
  - An auxiliary text file listing all tracked symbols (`AllSymbols_<StrategyName>.txt`).
- **Supported Symbols**: Main strategy security, manually provided lists, or identifiers loaded from a formatted text file.
- **Trading Activity**: None – the strategy only listens to market data and never calls any order methods.

## Parameters

- **Recording Interval** (`TimeSpan`, default 500 ms)
  - Interval between snapshots. Must be greater than zero.
- **Symbol Selection** (`MainSecurity` | `ManualList` | `FromFile`)
  - Chooses how the list of securities is built.
    - `MainSecurity`: record only the strategy's assigned `Security`.
    - `ManualList`: use the comma/semicolon separated `Symbols List`. The main security is added automatically if defined.
    - `FromFile`: load symbols from `Symbols File`.
- **Symbols List** (`string`)
  - Comma, semicolon, space or newline separated identifiers for additional instruments (used when `Symbol Selection = ManualList`).
- **Symbols File** (`string`, default `InputSymbolList.txt`)
  - Path to a text file containing the number of symbols on the first line followed by one identifier per line (used when `Symbol Selection = FromFile`). Relative paths are resolved against the output directory and then the current working directory.
- **Recording Format** (`Csv` | `Binary` | `All`)
  - Controls whether CSV files, binary files or both are created.
- **Time Format** (`Server` | `Local`)
  - Chooses between server time and local time stamps written to both CSV and binary outputs.
- **Output Directory** (`string`, default `<working dir>/ticks`)
  - Destination folder for generated files. Created automatically if missing.

## Workflow

1. Resolve the security list according to `Symbol Selection` (using `SecurityProvider.LookupById` when needed).
2. Validate that `Recording Interval` is positive and at least one security is available.
3. Create the output directory and write the `AllSymbols_<StrategyName>.txt` manifest.
4. Open CSV and/or binary writers for each security using the naming pattern `<symbol>_<strategy>.csv` or `.bin`.
5. Subscribe to Level1 data for every security and update an in-memory snapshot whenever bids, asks or last prices change.
6. A timer triggered every interval writes the latest snapshot to the corresponding files using either server or local timestamps.
7. On stop or reset all timers and file handles are disposed gracefully.

## Usage Notes

- Ensure the connector supplies Level1 data for all requested symbols; otherwise snapshots remain empty and nothing is written.
- The binary format stores Unix milliseconds plus flags indicating whether bid/ask/last values were present when recorded.
- When loading symbols from a file, the first line must be an integer count, mirroring the original MQL script format.
- Because no trading logic is executed, the strategy can be run alongside other trading strategies purely for data collection.
- Output file names are sanitized to remove unsupported characters (for example, replacing `:` with `_`).
