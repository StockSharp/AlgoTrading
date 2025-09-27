# Renko Live Chart Generator Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
`RenkoLiveChartGeneratorStrategy` is a faithful conversion of the MetaTrader 4 expert **RenkoLiveChart_v3_2.mq4**. The original script
produced an offline Renko chart by continuously writing synthetic bricks into a `.hst` file. Inside StockSharp the same idea is
implemented as a self-contained strategy: it subscribes to the configured candle feed, recreates the brick-building procedure
from the MQL version, and logs every completed Renko block. The strategy does not submit orders – its purpose is to generate a
live Renko representation that other components or operators can monitor.

## Mapping from the MQL Script
- **RenkoBoxSize** → `BrickSizeSteps`: both express the brick height as a multiple of the instrument price step. The strategy
  multiplies the value by `Security.PriceStep` to derive the absolute box size in price units.
- **RenkoBoxOffset** → `BrickOffsetSteps`: shifts the very first brick by the specified number of steps. The parameter is subject
  to the same validation as the MQL version: the absolute offset must be smaller than the brick size.
- **RenkoTimeFrame** → `CandleType`: StockSharp uses a strongly typed `DataType` instead of numeric time frame identifiers.
  The default subscribes to one-minute candles, but any available candle type (time frame, tick aggregation, range, etc.) can be
  selected.
- **ShowWicks** → `UseWicks`: when enabled, bricks include tails that extend to the extreme price reached before the brick was
  closed.
- **EmulateOnLineChart** → `EmulateLineChart`: toggles continuous logging of the currently forming brick so the operator can see
  the same incremental updates that MetaTrader displayed by forcing the offline chart to refresh.
- **StrangeSymbolName** → `UseShortSymbolName`: trims the symbol to the first six characters when producing log entries, matching
  the naming workaround used by the original expert.

## Parameters
| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BrickSizeSteps` | `int` | `10` | Renko brick height expressed in price steps. Converted to absolute price by multiplying with the security `PriceStep`. |
| `BrickOffsetSteps` | `int` | `0` | Offset (in price steps) applied to the very first brick. Keeps the starting range aligned with custom grid settings. |
| `UseWicks` | `bool` | `true` | If `true`, highs and lows may extend beyond the brick body, mirroring the optional wick handling in the MQL script. |
| `EmulateLineChart` | `bool` | `true` | Enables verbose logging of the active brick after each price update, similar to the line-chart refresh behaviour. |
| `UseShortSymbolName` | `bool` | `false` | Uses a six-character instrument alias when generating log prefixes, replicating the "StrangeSymbolName" toggle. |
| `CandleType` | `DataType` | `TimeFrame(1m)` | Candle source that seeds the Renko history. The strategy subscribes to this data set and replays its highs/lows. |

## Data Flow
1. `OnStarted` validates the configured brick size and offset, prepares the textual alias, and subscribes to two data streams:
   finished candles (specified by `CandleType`) and real-time trades.
2. `ProcessCandle` mirrors the historical processing loop from the MQL version. Each finished candle updates the working brick,
   checks for multi-brick breakouts, and emits as many synthetic Renko blocks as necessary.
3. `ProcessTrade` handles incoming ticks between candle closes. It keeps the running wick extremes, counts tick volume, and
   instantly closes or extends bricks when the live price crosses the next Renko boundary.
4. `EmitBrick` writes a detailed log entry that contains the new brick direction, OHLC values, accumulated tick volume, and the
   timestamp chosen to keep the sequence strictly increasing. When `EmulateLineChart` is `true`, `UpdateActiveBrick` also logs
   interim updates for the brick that is currently forming.

## Usage Tips
- Attach the strategy to any instrument supported by StockSharp and choose the candle source that should drive the Renko
  reconstruction (time frame, range, tick, volume, etc.).
- Enable `UseWicks` to replicate the "with shadows" mode of the original indicator. Disable it to force classic brick bodies.
- Keep `BrickOffsetSteps` within ±(`BrickSizeSteps` − 1). Larger offsets are rejected, exactly as in the MQL implementation.
- When `EmulateLineChart` is active, the strategy can be run side by side with trading strategies to provide a synchronized Renko
  perspective in the log window.

## Differences vs. the MQL Version
- File system interactions were removed. Instead of writing `.hst` files, the strategy exposes the Renko stream through StockSharp
  logging, which integrates with Designer, Shell, and other front-ends.
- The implementation relies on decimal arithmetic and StockSharp subscriptions rather than MT4 global arrays and timer loops.
- Time alignment uses `TimeSpan.FromMilliseconds(1)` increments to guarantee strictly increasing timestamps even when several
  bricks form on the same source candle.
- The script never submits orders. It serves as a Renko data generator that can be combined with custom analytics or manual
  monitoring.
