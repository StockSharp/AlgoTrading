# History Downloader Strategy

## Overview
The **History Downloader Strategy** is the StockSharp port of the MetaTrader expert advisor `HistoryDownloader.mq4`. The original
EA emulated `HOME` key presses on the chart window and relied on a companion indicator to expose the current chart statistics
through global variables. This C# version removes the platform-specific tricks and uses StockSharp's high-level market data API to
keep requesting older candles until the desired start date is reached. The strategy never sends orders – it is a pure data
acquisition utility intended to pre-load history for backtesting or chart analysis.

## How it works
1. When the strategy starts it subscribes to the configured timeframe (`CandleType`) for the instrument assigned to
   `Strategy.Security`.
2. Every finished candle updates the running statistics:
   - `_receivedCandles` counts all completed bars supplied by the connector;
   - `_earliestCandleTime` stores the oldest `OpenTime` seen so far;
   - the watchdog timestamp `_lastUpdateTime` is refreshed to indicate that new data arrived.
3. The built-in `Strategy.Timer` replaces the original busy loop. It fires every `RequestTimeout` interval:
   - if new data arrived during the interval, the timeout counter is reset;
   - otherwise the counter increments and, after `MaxFailures` consecutive intervals with no progress, the download stops with an
     error, mirroring the `MaxFailsInARow` behaviour from the MQL code.
4. Progress is written to the strategy log on every candle just like the chart `Comment` in MetaTrader. The helper method
   `FormatDuration` converts elapsed milliseconds into a human-readable `Xh Ym Zs` string, reproducing the `FormatTime` helper
   from the original project.
5. As soon as the oldest candle time drops below `TargetDate`, the strategy reports success, disposes the market data subscription
   and stops itself. The summary contains the number of received bars, the earliest timestamp and the total execution time.

## Monitoring and logging
- `LogInfo` displays the progress string with the bar count, earliest timestamp and elapsed time.
- `LogWarning` informs the user when the watchdog detects idle periods and shows the number of consecutive timeouts.
- `LogError` is used when the watchdog aborts the download after exceeding `MaxFailures`.
- The strategy never places trades, therefore `PnL`, `Position`, and order events remain untouched during execution.

## Parameters
| Name | Type | Default | MetaTrader counterpart | Description |
| --- | --- | --- | --- | --- |
| `CandleType` | `DataType` | 1-minute time frame | Chart period | Timeframe that should be downloaded from the connector. |
| `TargetDate` | `DateTimeOffset` | 2009-01-01 00:00:00Z | `ToDate` | Earliest acceptable candle open time. The strategy stops successfully once history reaches this timestamp. |
| `RequestTimeout` | `TimeSpan` | 1 second | `Timeout` (ms) | Maximum delay between candle updates before one failure is recorded. |
| `MaxFailures` | `int` | 10 | `MaxFailsInARow` | Consecutive timeouts tolerated before aborting the process. |

## Differences from the original expert advisor
- The Windows `PostMessage` calls and keyboard emulation were replaced with a native candle subscription.
- The indicator `HistoryDownloaderI.mq4` is not required: the strategy reads the candle stream directly and keeps the relevant
  counters internally.
- Instead of relying on terminal global variables, StockSharp timestamps and the watchdog timer provide deterministic progress
  tracking.
- Completion and failure are reported via strategy logs rather than modal alert boxes, making the tool suitable for unattended
  automation.

## Usage tips
- Assign a valid `Security`, `Portfolio`, and `Connector` before starting the strategy so the data feed can deliver historical
  candles.
- Adjust `RequestTimeout` to match the latency of your data source. Slow archival feeds might require a longer interval to avoid
  premature aborts.
- Increase `MaxFailures` when working with providers that deliver history in bursts or queue large requests.
- Because the strategy stops automatically after success or failure, it can be scheduled or triggered by other components in a
  workflow that prepares datasets for backtesting.
