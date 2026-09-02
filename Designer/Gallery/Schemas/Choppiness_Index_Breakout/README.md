# Choppiness Index Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The Choppiness Index does not say where the market is going, only whether it is going anywhere at all. This diagram uses it as a switch: while the index is low the market is trending and a position is opened on the side the close takes against a simple moving average; when the index climbs back into the sideways zone the position is given up, whatever it is worth.

![schema](schema.svg)

## Strategy Overview

- The Choppiness Index is computed over fourteen finished candles and reads as a percentage: low values mean a directional market, high values mean a range.
- A twenty-period simple moving average supplies the direction only; it is never a filter of its own, because the regime test has already decided whether trading is allowed.
- Entries are taken only from a flat position, so one trending stretch produces one trade instead of a growing pile of them.
- There is no stop and no target: the index that opened the trade is also the one that ends it.

## Entry and Exit Rules

- **Long entry**: The Choppiness Index is below the trending threshold, the candle closed above the simple moving average and the position is flat. The order buys one lot and opens a long.
- **Short entry**: The Choppiness Index is below the trending threshold, the candle closed below the simple moving average and the position is flat. The order sells one lot and opens a short.
- **Exit**: As soon as the Choppiness Index rises above the choppy threshold, the open position is closed: a long by a sell in close mode, a short by a buy in close mode. The original code carries no stop loss and no take profit either. Two things are deliberately different from that code. Its own thresholds are 99 and 99.5, which would leave the entry filter permanently open and the exit condition permanently unreachable, so the diagram uses the canonical 38.2 and 61.8 of the indicator's documentation instead, which is also what the strategy's own README describes. Its pause of five hundred bars between trades is left out as well, because a counter of that kind has no faithful equivalent in blocks.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that gives the entry its direction. |
| Choppiness Length | 14 | Averaging length of the Choppiness Index. |
| Trending Threshold | 38.2 | Index value the market has to stay below for an entry to be allowed. |
| Choppy Threshold | 61.8 | Index value above which the market counts as sideways and the position is closed. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on; the original uses one-minute candles, this diagram the five-minute candles of the packaged history. |

## Diagram Details

- The candle block feeds the Choppiness Index, the moving average and a converter that pulls the closing price out of the candle.
- Two comparisons turn the index into two regime flags — trending below one threshold, sideways above the other — and two more compare the close with the moving average.
- The position block is compared with a zero constant three times, giving a flat guard for the entries and a long and a short guard for the exits.
- Four logical ANDs feed four position modify blocks: two open a position and take their volume from the shared constant, two only close what is already there.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
