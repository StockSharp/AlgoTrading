[Русский](README_ru.md) | [中文](README_cn.md)

# Get M1 Data Strategy

This strategy replicates the MetaTrader helper that exported one-minute bars during testing.
It subscribes to finished candles, accumulates their values, and writes them into files when the strategy stops.
The exporter works both in backtests and in live mode, allowing you to produce clean historical snapshots
without relying on platform-specific `.hst` files.

## How it works

1. When the strategy starts it subscribes to the configured candle type (one minute by default).
2. Each finished candle is stored together with open, high, low, close, and volume information.
3. On stop the collected data is written to disk in one or two formats depending on the parameters.
4. After the files are saved the in-memory buffer is cleared so the strategy can be restarted safely.

The implementation follows the high-level StockSharp API. Candles are delivered through `SubscribeCandles`
and processed in `ProcessCandle`, while file management is delayed until `OnStopped` to avoid blocking
real-time execution.

## Parameters

- **File Name** – Base path used for generated files. A `.csv` and/or `.bin` extension is appended automatically.
  Relative paths are resolved against the current working directory, and missing folders are created.
- **Candle Type** – Data type requested from the connector. Defaults to a one-minute time frame but can be changed
  to export other resolutions.
- **Write CSV** – Enables text export with a header row and comma-separated values. Suitable for spreadsheets,
  Python notebooks, or manual inspection.
- **Write Binary** – Produces a compact binary snapshot. Each record stores the candle time (UTC `DateTime` binary form),
  prices, and volume, preceded by a simple header `(version = 1, count = N)` for validation.

## Exported file structure

### CSV file

The CSV file is UTF-8 encoded without BOM and uses the following columns:

| Column | Description |
| --- | --- |
| Time | Candle open time in `yyyy-MM-dd HH:mm:ss` format (UTC offset preserved). |
| Open | Open price formatted with invariant culture. |
| High | High price formatted with invariant culture. |
| Low | Low price formatted with invariant culture. |
| Close | Close price formatted with invariant culture. |
| Volume | Total or trade volume captured for the interval. |

### Binary file

The binary file begins with two integers (`version`, `count`), followed by a packed stream for each candle:

1. `long` – `DateTime.UtcDateTime.ToBinary()` representation of the open time.
2. `decimal` – open price.
3. `decimal` – high price.
4. `decimal` – low price.
5. `decimal` – close price.
6. `decimal` – volume (uses `TotalVolume` if available, otherwise falls back to `Volume`).

This layout is intentionally simple so that external tools can parse it without dependencies.

## Usage notes

- Run the strategy with the desired instrument and time range, then stop it to trigger the export.
- If both `WriteCsv` and `WriteBinary` are disabled the logic logs an informational message and skips disk access.
- When volume information is missing from `TotalVolume`, the code falls back to `Volume` to mimic
  the behaviour of the original MQL script that exported tick volume.
- To create multiple files, launch the strategy repeatedly with different `FileName` values.
- The exporter does not place trades and therefore can be combined with other analytical strategies in a session.

