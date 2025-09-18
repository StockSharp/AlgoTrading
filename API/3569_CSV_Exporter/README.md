# 3569 CSV Exporter

## Overview
The strategy is a high-level StockSharp port of the MetaTrader expert advisor `MQL/41254/CSV Exporter.mq5`. The original script periodically exported the latest candle closes into a CSV file for external processing. The StockSharp version keeps the behaviour intact by subscribing to finished candles, buffering their close prices, and rewriting a CSV file on the desired schedule.

The strategy does not place or manage orders. Its sole purpose is to generate a continuously updated price file that other tools can consume.

## Strategy logic
1. **Candle subscription** – When started, the strategy subscribes to the configured candle type (default 5-minute candles) and listens for finished bars only, avoiding partial data.
2. **Ring buffer** – Close prices are stored inside a fixed-size ring buffer whose length matches `CandleCount`. The buffer always contains the most recent completed candles.
3. **Timed exports** – After an initial one-second delay, the strategy performs an export every `UpdateInterval`. Each export rewrites the CSV file with the current buffer contents ordered from oldest to newest.
4. **File naming** – The output file name is automatically assembled from the security identifier and the candle time frame (for example `AAPL@NASDAQ_TF_300s.csv`). Invalid filename characters are replaced with underscores.
5. **Status reporting** – After each export the strategy logs and exposes a textual status message describing how many records were written and where the file is located.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `CandleType` | Candle type processed by the strategy. Defaults to 5-minute time frame candles. |
| `CandleCount` | Number of completed candles written to the CSV file. |
| `UpdateInterval` | Time interval between consecutive exports. |
| `ExportDirectory` | Directory path where the CSV file will be saved. |

## Usage notes
- Assign a valid `Security` before starting the strategy. The identifier is used in the output file name.
- Ensure the account running the strategy has write permissions to `ExportDirectory`. The directory is created automatically if it does not exist.
- Adjust `CandleCount` and `UpdateInterval` to control how much history each file contains and how frequently it is refreshed.
- The strategy overwrites the CSV file on every export, matching the behaviour of the MetaTrader expert that rewrote the same file repeatedly.
- Access the latest export message through the `LastExportStatus` property for integration with dashboards or logs.

## Differences versus the MetaTrader version
- MetaTrader relied on `CopyRates` to pull historical bars on demand. StockSharp keeps a live ring buffer updated via real-time candle subscriptions.
- File operations use the .NET `StreamWriter` API with UTF-8 encoding instead of the MetaTrader `FileOpen` function.
- The export timer leverages candle timestamps instead of `OnTick` timers, which is the idiomatic approach within StockSharp strategies.
- Security and directory validation is explicit, providing clear error messages when prerequisites are not met.
