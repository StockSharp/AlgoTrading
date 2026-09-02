# Hull MA Slope Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The Hull Moving Average follows price with very little lag, so the direction of its own slope is already a trend signal. The diagram measures how far the average moved since the previous candle, as a fraction of its own value, and turns the position to that side once the move is larger than a small threshold. The original counts 500 one-minute candles; here the length is 100 five-minute candles, the same stretch of time on the packaged history.

![schema](schema.svg)

## Strategy Overview

- Only the slope of the Hull Moving Average is traded — the price itself is never compared with the average.
- The slope is relative, expressed as a fraction of the previous value, so the same threshold works at any price level.
- Above +0.02% the diagram wants to be long, below -0.02% short; inside that band nothing happens and the open position is kept.
- After the first signal the strategy is always in the market: there is no stop, no target and no flat state between trades, exactly as in the original code.

## Entry and Exit Rules

- **Long entry**: The Hull Moving Average rose by more than the rise threshold since the previous candle and the position is not long. The order buys the shared volume plus the size of an open short, so one order reverses the position.
- **Short entry**: The Hull Moving Average fell by more than the fall threshold since the previous candle and the position is not short. The order sells the shared volume plus the size of an open long.
- **Exit**: There is no exit block: the opposite slope signal reverses the position, and because the order volume already contains the absolute position, a single market order closes one side and opens the other.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Hull MA Length | 100 | Length of the Hull Moving Average, scaled from 500 one-minute candles to 100 five-minute ones. |
| Rise Threshold | 0.0002 | Relative rise of the average over one candle that opens a long; 0.0002 is 0.02%. |
| Fall Threshold | -0.0002 | Relative fall of the average over one candle that opens a short; the mirror image of the rise threshold. |
| Volume | 1 | Order volume, in lots, before the open position is added to it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- A previous-value block keeps the Hull average of the previous candle, and it stays silent on the first value, which reproduces the skipped first bar of the original.
- The slope formula subtracts the previous value from the current one and divides by the previous value, which turns the move into a fraction.
- Two comparisons split that fraction into three states with the positive and the negative threshold constant.
- Each logical AND joins one slope condition with a position check, and the volume formula adds the absolute position to the shared volume, which is what turns an entry into a reversal.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
