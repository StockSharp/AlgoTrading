# Calendar Change Saver Strategy

## Overview
The **Calendar Change Saver Strategy** is a StockSharp high-level strategy that replicates the behavior of the original MQL5 service from package `MQL/45592`. The service monitors the MetaTrader economic calendar and persists every batch of new change identifiers to disk. The converted strategy keeps the same idea: it listens for news updates supplied by the connected broker and serializes the received headlines to a log file for further offline processing.

The strategy does **not** place any trades. Its sole purpose is to build a persistent audit trail of calendar changes so that other systems can replay the news flow and backtest calendar-driven trading rules.

## Conversion Notes
- The MQL5 version repeatedly called `CalendarValueLast` in a timed loop, saved each returned batch of identifiers to a binary file, and ignored suspiciously large responses (more than 100 records).
- In StockSharp we use an event-driven approach. The strategy subscribes to `MarketDataTypes.News` for the selected security and caches every received `NewsMessage`.
- A timer flushes the cached messages to disk at a configurable interval. Each file line stores the flush time, batch size, and a compact serialization of the captured news items.
- Oversized batches are skipped just like in MQL5, protecting the history from corrupt or duplicated terminal responses.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `OutputFileName` | Path to the text file that will contain serialized news batches. Intermediate directories are created automatically. | `calendar_changes.log` |
| `FlushIntervalMilliseconds` | Polling interval for the flush timer. Lower values write more often, higher values reduce disk usage. | `1000` |
| `BulkLimit` | Maximum amount of news entries that can be written in a single batch. Bigger batches are treated as unreliable and skipped. | `100` |

## Output Format
Each line in the log file follows the structure:

```
<flush_time_iso>|<batch_count>|[<event1_time_iso>;<source>;<headline>;<story>,<event2_time_iso>;...]
```

- `flush_time_iso` – UTC timestamp of the flush moment.
- `batch_count` – number of news records flushed.
- `event_time_iso` – UTC timestamp of the news item.
- `source`, `headline`, `story` – sanitized text fields (all separators are replaced with spaces).

## Usage
1. Attach the strategy to a connector that supports news delivery.
2. Select the target security and configure the parameters if needed (for example, adjust the destination file or the flush interval).
3. Start the strategy. Whenever the connector reports new `NewsMessage` instances, they will be queued and periodically written to the log file.
4. Stop the strategy to close the file handle gracefully.

## Limitations
- The output only contains news metadata provided by the data feed. If the feed does not supply calendar events, the file will stay empty.
- News batches larger than the configured bulk limit are ignored to mimic the protection logic from the MQL5 service.

