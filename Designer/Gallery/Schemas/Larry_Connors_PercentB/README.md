# Larry Connors Bollinger %B Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A long-only mean-reversion diagram built on Bollinger %B, the position of the close inside the Bollinger band expressed as a percentage of the band width. The idea of Larry Connors is that a single weak candle proves nothing, so the diagram waits for %B to stay in the lower part of the band on two candles in a row before it buys, and holds until %B recovers into the upper part.

![schema](schema.svg)

## Strategy Overview

- The BollingerPercentB indicator does in one block what the original strategy computes by hand from the bands; its scale is 0 to 100, so the classic 0.35 and 0.8 thresholds are written as 35 and 80.
- A previous-value block keeps the reading of the last candle, which is what turns a single weak candle into a two-candle condition.
- The strategy is long only: it buys weakness and sells that same long back, never opening a short.
- The position takes part in both decisions, so the entry cannot stack and the exit cannot fire on a flat book.

## Entry and Exit Rules

- **Long entry**: %B of the previous candle and %B of the current candle are both below the low threshold, and the position is not long. The order buys one lot.
- **Short entry**: The diagram never sells short. The sell block only serves as the exit of an open long.
- **Exit**: %B rises above the high threshold while the position is long. The order sells the same one lot and brings the position back to flat.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Bollinger Period | 20 | Length of the Bollinger bands the %B reading is built on. |
| Bollinger Deviation | 2 | Standard deviation multiplier of the Bollinger bands. |
| Low %B | 35 | Threshold below which %B counts as the lower part of the band; it has to hold for two candles. |
| High %B | 80 | Threshold above which %B counts as recovered, which closes the long. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the indicator block, whose value goes both into the comparisons and into the previous-value block.
- Two comparisons against the same low constant give the current and the one-candle-old condition; a third compares %B with the high constant for the exit.
- Two more comparisons check the position against zero: not long for the entry, long for the exit.
- The two logical AND blocks trigger the position modify blocks, both of which take their volume from a single shared constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
