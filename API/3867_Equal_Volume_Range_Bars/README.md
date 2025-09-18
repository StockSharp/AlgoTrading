# Equal Volume & Range Bars

## Overview
Equal Volume & Range Bars ports the MetaTrader 4 script `equalvolumebars.mq4` to StockSharp. The original script generated off-line charts whose candles closed after either a fixed number of ticks or after the price had traversed a configurable point range. The strategy reproduces the same candle-building logic inside the StockSharp environment: it listens to live ticks, optionally preloads historical M1 candles, and emits detailed log entries whenever a synthetic bar is completed.

## Candle construction logic
* **Dual operating modes** – `EqualVolumeBars` closes the bar once the accumulated tick volume exceeds the configured threshold, while `RangeBars` requires the candle's high-low range (measured in security price steps) to exceed the same numeric threshold.
* **Tick-driven updates** – every trade update refreshes the current candle high, low, close, and tick volume. When the threshold would be exceeded, the strategy finalizes the previous candle with the existing statistics and immediately starts a fresh bar with the current tick as its first entry.
* **Minute history seeding (optional)** – when `FromMinuteHistory` is enabled, the strategy replays finished M1 candles as a sequence of synthetic ticks (open → intermediate extremes → close). This approximates the offline chart's initialization step without requiring external CSV tick files.
* **Monotonic timestamps** – the builder enforces strictly increasing timestamps so that log consumers or downstream modules can load the data without encountering duplicate time keys.

## Parameters
* **Work Mode** – selects between `EqualVolumeBars` and `RangeBars` candle construction.
* **Ticks In Bar** – number of ticks per candle (equal volume mode) or point range measured in price steps (range mode).
* **Use Minute History** – enables the synthetic replay of finished M1 candles before live ticks arrive.
* **Minute Candle Type** – candle subscription used for the historical seeding step (defaults to one-minute time frame).

## Additional notes
* The strategy infers the point size from `Security.PriceStep` (falling back to `Security.MinPriceStep` or `0.0001` when no metadata is available) to mirror the `_Point` constant used by MetaTrader.
* Instead of writing `.hst` files and refreshing a chart window, the C# port logs every finished candle with full OHLCV data, making it easy to feed another component or to compare results with the MT4 offline chart builder.
* No orders are ever submitted; the class focuses exclusively on data transformation just like the original script.
* Only the C# version is provided. A Python version and folder are intentionally omitted per the conversion requirements.
