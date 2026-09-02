# Turnaround Tuesday Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The idea is the turnaround after a bad session: a session that ends lower than it started often hands the next one a bounce, so the diagram waits for the market to recover above its moving average and buys that recovery, and mirrors the whole thing after a session that closed higher. Despite the name, the original strategy contains no weekday filter at all, and neither does this diagram.

![schema](schema.svg)

## Strategy Overview

- Two candle series work side by side: a session series decides which way to lean and a faster trading series times the entry.
- The session verdict is a single comparison of the session candle's close with its own open, so no state has to be remembered between candles.
- The simple moving average on the trading series is the confirmation: after a losing session the diagram buys only once the price has climbed back above the average.
- Because the session verdict arrives once per session candle, the logical AND can fire only once per session, which is exactly the one-entry-per-session rule of the original.

## Entry and Exit Rules

- **Long entry**: The last session closed below its open, the trading candle closes above the simple moving average and the position is flat. The order buys the shared volume at market.
- **Short entry**: The last session closed above its open, the trading candle closes below the simple moving average and the position is flat. The order sells the shared volume at market.
- **Exit**: The position is left on the side of the average, not on a target: a close back below the average closes a long, a close back above it closes a short. There is no stop loss and no take profit, exactly as in the original strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| MA Period | 20 | Length of the simple moving average that confirms the turn on the trading series. |
| Volume | 1 | Order volume, in lots. |
| Trading candles | 00:05:00 | Time frame the entries and exits are timed on. |

## Diagram Details

- The session candle block feeds two converters, one for the open and one for the close, and the two comparisons between them give the declined and rallied flags.
- The trading candle block feeds the moving average and a converter for the closing price; two comparisons place that close on one side of the average or the other.
- Each logical AND joins a session flag, a side-of-the-average flag and the flat-position check before triggering an entry block that carries the open-position condition.
- The exit blocks hang directly on the two average comparisons and carry the close-position condition, so each of them only ever flattens its own side.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
