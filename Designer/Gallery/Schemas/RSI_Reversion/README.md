# RSI Reversion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The diagram fades an RSI extreme, but only at the moment the index turns back: it buys when RSI climbs back above the oversold level after having been under it, and sells when RSI falls back below the overbought level. One order carries the volume needed to flip the position, so the diagram is either flat or on exactly one side.

![schema](schema.svg)

## Strategy Overview

- RelativeStrengthIndex works on finished candles, and a previous-value block keeps the reading of the candle before, so the pair sees the exact candle on which the index re-enters the normal range.
- A SimpleMovingAverage of 50 candles is kept from the original strategy: it never picks a side, it only holds trading back until it is formed.
- The current position takes part in both decisions, and the order volume is the base volume plus the open position, so a single market order closes and reverses in one step.

## Entry and Exit Rules

- **Long entry**: The previous RSI reading is below the oversold level, the current one is at or above it, the SMA 50 is formed and the position is not long. The order buys the base volume plus the size of an open short, which turns a short into a long or opens a long from flat.
- **Short entry**: The previous RSI reading is above the overbought level, the current one is at or below it, the SMA 50 is formed and the position is not short. The order sells the base volume plus the size of an open long, which turns a long into a short or opens a short from flat.
- **Exit**: There is no separate exit block: the opposite reversion signal closes the position and opens the other side with the same order. The original strategy has neither stop loss nor take profit, and its ten-candle pause after a trade is not carried over, because the diagram elements do not hold a level between candles.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| RSI Length | 14 | Averaging length of the Relative Strength Index. |
| SMA Length | 50 | Length of the Simple Moving Average that gates the warm-up. |
| Oversold | 30 | Level the index has to come back above for a long. |
| Overbought | 70 | Level the index has to come back below for a short. |
| Volume | 1 | Base order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both indicators; a previous-value block on the RSI output supplies the reading of the candle before.
- Two comparison blocks per side test the previous and the current reading against the level constant, which reproduces the condition of the source code literally.
- The comparison of the SMA against zero stands for the guard in the source code; since the indicator block emits formed values only, trading starts once fifty candles are in.
- A formula block adds the absolute position to the volume constant, and both position modify blocks send market orders with that volume.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
