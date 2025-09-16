# FXT ticks Collector Strategy

This strategy converts the MetaTrader 4 expert advisor `FXTticksCollector.mq4` into the StockSharp framework. The original script continuously appends market ticks to a MetaTrader FXT tester file. The C# port preserves that idea by writing a compact binary stream that mirrors the MT4 layout while relying on StockSharp subscriptions for candles and trades.

## Behaviour

1. **Initialization**
   - The strategy sanitizes the current security identifier and combines it with the selected timeframe to form `<Symbol>_<Timeframe>_0.fxt`.
   - If the file already exists and its header matches the current configuration, the strategy appends new data; otherwise, the file is recreated from scratch.
   - When a new file is created the first `InitialBarCount` finished candles are written as historical bars, replicating the "first 100 bars" seed from the MQL version.

2. **Real-time recording**
   - A candle subscription keeps track of the most recent bar. Every finished candle that arrives before real-time ticks (for example, historical backfill after a restart) is written with flag `0` (prefill) or `1` (gap fill).
   - The trade subscription stores every incoming tick. Each tick writes a snapshot that contains the aligned bar open time, OHLC values taken from the current candle (falling back to the trade price), total volume, the actual tick time, and the MetaTrader flag value `4`.
   - Bar counters are automatically updated whenever a new aligned bar appears so that the header always reflects the real number of bars processed.

3. **Shutdown**
   - On stop the header is rewritten with the final number of bars, tick records, and the open time of the last processed bar. All streams are flushed and the totals are logged.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Timeframe that defines bar alignment and file naming. | 1 minute candles |
| `InitialBarCount` | Number of finished candles written when creating a new file. | 100 |
| `FileDirectory` | Directory where the FXT-style binary file is stored. | `.` |
| `AppendToExisting` | When `true`, the strategy appends to an existing file if the header matches symbol, timeframe, and warm-up settings. | `true` |

## Binary file layout

The produced file keeps the MetaTrader layout in spirit while documenting the structure to make automated processing easier:

- **Header** (little endian)
  1. `int` magic number `0x46585443` (`"CXTF"`).
  2. `int` version (`1`).
  3. `string` security identifier (BinaryWriter string with 7-bit length prefix).
  4. `long` timeframe in whole seconds.
  5. `int` warm-up candle count written during creation.
  6. `int` number of bars processed so far.
  7. `long` number of tick snapshots stored.
  8. `long` open time of the most recent bar (Unix seconds).
  9. `bool` copy of the append setting used when the file was created.

- **Records**
  - *Prefill / gap bars*: open time, open/low/high/close (double), total volume (double), close time, flag `0` or `1` (prefill or backfill). These records allow the file to contain bars that were produced while the strategy was offline.
  - *Tick snapshots*: aligned bar open time, open/low/high/close, aggregated volume, tick time, flag `4` exactly as in the MQL collector.

## Conversion notes

- The original EA depends on `FXTHeader.mqh`. The C# port implements an equivalent header that carries the same metadata but is explicitly documented so that external tools can parse it without MT4 helpers.
- MetaTrader stored the current bar using the `Time[0]` arrays. StockSharp does not expose those arrays directly, therefore the strategy keeps the latest candle message and aligns tick times to the configured timeframe. This guarantees that bar boundaries stay consistent even if candle updates arrive after ticks.
- File names now use sanitized security identifiers (`/`, `:` and other invalid characters are replaced with `_`) to remain portable on any operating system.
- The strategy does not create orders. It is intended for data collection only, so it can safely run in conjunction with other trading strategies.

## Usage tips

1. Assign a security and a portfolio, start the strategy, and let it run while the market is active.
2. If you need historical bars at the beginning of the file, make sure your data provider is able to deliver finished candles during the warm-up phase.
3. Restarting with the same parameters will append data to the existing file. Changing the timeframe, symbol, or warm-up value forces a brand-new file to be created, matching the behaviour of the MT4 script when the header does not match.
