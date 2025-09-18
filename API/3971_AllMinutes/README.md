# AllMinutes Strategy

## Overview
The **AllMinutes Strategy** is a port of the MetaTrader 4 script `AllMinutes.mq4`. The original MQL tool synchronized multiple offline minute charts by continuously rewriting `.hst` history files. This StockSharp version reproduces that workflow with the high-level API: it subscribes to the configured instruments, aligns incoming candles to their target time frame, fills missing bars with synthetic data, and saves the result to MetaTrader-compatible HST files.

The strategy is intended for advanced data maintenance pipelines where a StockSharp connector feeds tick or minute data into a MetaTrader testing environment. It does not place orders or manage positions.

## Key Features
- Supports an arbitrary list of symbol/time-frame pairs through a single comma-separated parameter.
- Generates **ALL&lt;symbol&gt;&lt;timeframe&gt;.hst** files and writes the MT4 build 509 (version 401) header layout.
- Detects gaps between finished candles and fills them with flat bars that repeat the last known close, optionally skipping weekend timestamps.
- Periodically flushes file buffers via the strategy timer to keep the HST file synchronized with MetaTrader readers.
- Allows partial updates: when the current bar is still forming (`CandleStates.Active`), the most recent record is rewritten instead of appending a duplicate.

## Parameters
| Parameter | Description |
|-----------|-------------|
| `ChartList` | Comma-separated list with entries like `EURUSD@FXCM 1` (symbol plus minute-based time frame). The board suffix is optional when the symbol can be resolved via the assigned `SecurityProvider`. |
| `SkipWeekends` | When enabled, gap-filling ignores Saturday/Sunday timestamps and late Friday rollover minutes, matching the original script logic. |
| `RefreshInterval` | Timer interval (milliseconds) used to flush the file streams. Lower values make the HST files update faster at the cost of more disk I/O. |
| `OutputDirectory` | Destination folder where the generated HST files are created. Default is `./AllMinutes` relative to the current process. |

## Workflow Details
1. **Initialization**: each entry in `ChartList` resolves to a StockSharp `Security` and a candle subscription with the requested minute frame. The strategy opens (or creates) the target HST file and writes the standard header.
2. **Subscription**: candles are requested in history+live mode. Finished candles append new records, while active candles rewrite the last record to keep the current minute up to date.
3. **Gap filling**: before every append, the strategy compares the aligned open time with the previous one. If gaps longer than the configured time frame are found, synthetic flat bars are inserted. Those bars repeat the last close price and use a tick volume of `1`, mirroring the MQL implementation.
4. **Weekend filter**: when `SkipWeekends` is true, all filler bars whose UTC timestamp falls on Saturday, Sunday, or the 23:00 Friday rollover window are skipped.
5. **Flushing**: a strategy timer calls `Flush()` on every open file stream, ensuring that external readers can see the newest data without waiting for the stream to be disposed.

## Usage Notes
- Assign a working `SecurityProvider` (for example from a `Connector`) before starting the strategy so that symbol identifiers can be resolved.
- The strategy does not manipulate `Portfolio` or orders. It can run side by side with other trading strategies that share the same market data source.
- Generated HST files can be copied to `MetaTrader 4\history\<broker>` to feed offline charts or tester data.
- File names follow the original convention: `ALL{symbolCode}{timeframe}.hst`. Any characters that are not valid for file names are replaced with `_`.

## Differences from the MQL Version
- StockSharp handles candle subscriptions via the event-driven high-level API, whereas the MQL script used timers plus direct history queries.
- Disk I/O is performed with .NET `FileStream`/`BinaryWriter` classes but respects the MT4 header and record layout for compatibility.
- The strategy rewrites the most recent record whenever an active candle update arrives; the MQL version accomplished the same effect inside the `OnTimer` handler.
- Error handling surfaces through `InvalidOperationException` when configuration is invalid (e.g., unknown symbol), providing clearer diagnostics during deployment.

## Limitations
- The strategy assumes UTC timestamps when skipping weekends. If the data source uses a different session calendar, adjust the weekend filter accordingly.
- Only minute-based time frames are supported because the original script accepted minutes. Extend `ChartList` parsing if other candle types are required.
- The generated files are always encoded as MT4 build 509 (version 401) history files. MetaTrader 5 HST2 format is not produced.

