# Piano Multi Timeframe Bar State Strategy

## Overview
This strategy reproduces the logic of the original **Piano.mq5** script by Vladimir Karputov. Instead of drawing a canvas panel inside MetaTrader, the StockSharp port observes the bullish or bearish state of the latest finished candles across twenty-one different timeframes. The information is aggregated into three formatted strings that mirror the rows drawn by the indicator (most recent bar, previous bar, and the second previous bar). The strings are exposed through public properties and written to the strategy log whenever one of the timeframes delivers a finished candle.

The strategy contains **no trading logic**. It serves as a monitoring component that allows designers or runners to inspect synchronized multi-timeframe sentiment for a single security.

## Relation to the MQL version
| Aspect | MQL5 version | StockSharp port |
| --- | --- | --- |
| Visualisation | Custom Canvas label | Text lines stored in properties and logged |
| Timeframes | M1…MN1 array (21 items) | `TimeSpan` subscriptions for the same set |
| Bar state encoding | `1` bullish, `0` bearish, `-` neutral | Identical encoding |
| Data access | `CopyRates` per timeframe | `SubscribeCandles` per timeframe |
| Execution mode | Indicator, no orders | Strategy without order methods |

## Implementation details
1. **Subscriptions** – `SubscribeCandles` is called for every timeframe defined in the `TimeFrames` array. Each subscription forwards finished candles to a single processing method through the high-level API.
2. **Bar classification** – `DetermineStatus` compares the open and close price of the candle and assigns `1`, `0`, or `-`. Partial data automatically resolves to `-`, keeping behaviour aligned with the MQL script when history is unavailable.
3. **Rolling memory** – Every timeframe keeps a small `PianoBuffer` (array based circular buffer) that stores a configurable number of recent states (three by default). Only tabs are used for indentation to comply with repository standards.
4. **Aggregation** – `UpdateLines` builds three text rows by iterating over all buffers. The routine formats the output exactly as the original `text1`, `text2`, and `text3` strings (`*` separator followed by the status code).
5. **Logging and exposure** – The strings are published via `LatestCurrentBarLine`, `LatestPreviousBarLine`, and `LatestSecondPreviousBarLine`. Every update is also pushed to the strategy log for quick inspection while running inside Designer or Runner.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BarsToTrack` | `int` | `3` | Number of finished candles to keep per timeframe. The value controls the depth of the buffers and therefore the number of characters rendered per output line. Optimisation bounds are set to 1–5 with step 1. |

## Outputs
- **LatestCurrentBarLine** – sequence of `*X` tokens describing the most recent finished candle on each timeframe (`X` is `1`, `0`, or `-`).
- **LatestPreviousBarLine** – the same format for the candle preceding the latest one.
- **LatestSecondPreviousBarLine** – the oldest candle held inside the buffer (third row by default).
- **Log entries** – every finished candle triggers a log line: `"[H1] Piano states updated. Latest: *1…"`. The log explicitly states which timeframe produced the change.

## Usage guidelines
1. Attach the strategy to a security and ensure historical data for all timeframes is available. The buffers initialise to `-` until enough candles are received.
2. Monitor the three string properties or the strategy log to understand how different timeframes align. The format allows straightforward visual comparison or further processing in custom dashboards.
3. Adjust `BarsToTrack` when a longer or shorter history window is desired. Increasing the value expands the buffer and the resulting strings without altering logic.
4. Because the component does not send orders, it can safely run alongside trading strategies to provide contextual market state information.

## Notes and limitations
- Weekly and monthly data rely on `TimeSpan.FromDays(7)` and `TimeSpan.FromDays(30)` subscriptions, which match the cadence used by many StockSharp samples.
- The strategy assumes the selected security supports the requested timeframes. Missing subscriptions will leave placeholder `-` markers in the output.
- Users may forward the string properties to custom UI widgets or dashboards for richer visualisation if desired.

