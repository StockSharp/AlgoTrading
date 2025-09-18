# Ticks Strategy

## Overview

The **Ticks Strategy** is a C# conversion of the MetaTrader 4 expert *Ticks.mq4*. The original script grouped a fixed number of
bid ticks into synthetic OHLC entries and stored them in a CSV file. This StockSharp port reproduces the same behaviour by
listening to best bid updates from the current security, aggregating them into batches of configurable size, and exporting each
completed batch as one line of comma-separated values.

Compared to the source script the strategy adds practical improvements:

* Automatic creation of the output directory and configurable file name.
* Safe disposal of file handles and subscriptions when the strategy stops or is reset.
* English inline comments that explain the data flow for easier maintenance.

## Data flow

1. On start the strategy validates that a `Security` is assigned, resolves the target file name, and opens a UTF-8 encoded CSV
   writer. The default file name follows the MetaTrader pattern `<symbol> volume <ticks>.csv`.
2. It subscribes to Level1 data and processes every update that contains a `BestBidPrice` change, treating it as the next tick in
the batch.
3. The first tick in a batch defines the open price and timestamp; subsequent ticks expand the high/low range. When the configured
   number of ticks is reached, the last bid becomes the close price and the full record is written to disk as
   `date,time,open,high,low,close,count` using invariant culture formatting.
4. After the row is flushed the internal counters reset and the next batch starts with the following tick.

The resulting CSV file therefore mirrors the original MetaTrader output where each line aggregates exactly `TicksPerBatch` bid
updates.

## Parameters

| Name | Description |
|------|-------------|
| `TicksPerBatch` | Number of bid ticks that triggers a CSV write. The default value is 100, matching the MQL `volume` input. |
| `OutputDirectory` | Optional folder for the CSV file. Leave empty to use the current working directory. |
| `CustomFileName` | Optional file name override without directory information. When empty the MetaTrader naming scheme is used. |

All parameters are configured through `StrategyParam<T>` instances so they can be changed from the UI or during optimisation.

## Output format

Each written row contains seven fields separated by commas:

```
YYYY.MM.DD,HH:MM,open,high,low,close,count
```

* `YYYY.MM.DD` and `HH:MM` come from the timestamp of the first tick in the batch.
* `open`, `high`, `low`, and `close` are the bid prices tracked during the batch.
* `count` equals the configured tick batch size and confirms how many updates were included.

## Usage notes

* Assign both `Portfolio` and `Security` before starting the strategy. Only the main security is used and no additional
  instruments are resolved.
* The output directory is created automatically if it does not exist. Existing files with the same name are overwritten, matching
  the MetaTrader behaviour of deleting the previous CSV on start.
* The Level1 subscription is disposed and the file writer is closed during stop or reset, preventing handle leaks when the
  strategy is started multiple times.
* The code relies solely on best bid updates. If the data provider does not supply bid prices the CSV will remain empty, just as
the original expert required the MetaTrader terminal to receive fresh ticks.

## Differences from the MQL4 version

* The StockSharp version uses strongly-typed parameters instead of raw extern inputs.
* File handling is explicit with `StreamWriter` and UTF-8 encoding; the MQL script relied on MetaTrader defaults.
* Additional guard clauses and exception messages make failure scenarios clearer when the environment is not ready.
