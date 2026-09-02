# ADX Trend MA Crossover Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The diagram trades the candle that steps over a long simple moving average, but only while ADX says the market is really trending. A candle counts as a crossing when it opened on one side of the average and closed on the other, and the position is then turned to the side the candle closed on. The original runs on one-minute candles; this diagram uses the five-minute candles of the packaged history.

![schema](schema.svg)

## Strategy Overview

- The 200-period SMA is the reference line, and a previous-value block keeps the value it had one candle earlier, so the open is measured against the average of its own bar and the close against the current one.
- An exclusive OR of these two comparisons is true exactly on the bars that straddle the average — this is the crossing test of the original code, not a crossing of two indicator lines.
- ADX with a length of fifty gates every entry: a candle that crosses the average in a quiet market is ignored.
- There is no stop and no target — the position is only turned around by the opposite crossing, and the order volume is the shared volume plus whatever is already held.

## Entry and Exit Rules

- **Long entry**: ADX is above the threshold, the candle crossed the moving average, the close is above the current SMA and the position is not long. The order buys the shared volume plus the size of an open short, so one order closes the short and opens the long.
- **Short entry**: ADX is above the threshold, the candle crossed the moving average, the close is at or below the current SMA and the position is not short. The order sells the shared volume plus the size of an open long.
- **Exit**: There is no separate exit: a position is held until the opposite crossing reverses it, exactly as in the original code, which implements neither a stop loss nor a take profit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| ADX Length | 50 | Averaging length of the Average Directional Index. |
| ADX Threshold | 25 | ADX value the market has to exceed for an entry to be allowed. |
| SMA Length | 200 | Length of the simple moving average the candles are measured against. |
| Volume | 1 | Order volume, in lots, before the open position is added to it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- Two converters read the open and the close of every finished candle, while the moving average and ADX are calculated on the candle itself.
- A previous-value block delays the SMA by one candle; the two comparisons that use it and the current value are joined by an exclusive OR, which is the crossing test.
- A logical NOT turns the 'close above the average' condition into the short-side condition, so a single comparison serves both directions.
- A formula block adds the absolute position to the shared volume, which lets one market order close the old side and open the new one in a single step.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
