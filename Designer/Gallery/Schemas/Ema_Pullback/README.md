# EMA Pullback Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A trend diagram that refuses to buy a breakout. The two exponential moving averages decide the direction, and the entry waits for the close to come back and touch the fast average, so the position is opened at a better price inside a move that is already running. The trend itself decides the exit: the position is closed as soon as the averages swap places.

![schema](schema.svg)

## Strategy Overview

- Two exponential moving averages of the close, a fast one of 8 and a slow one of 21, define which side the diagram is allowed to trade.
- A crossing block watches the close against the fast average, so the pullback is caught on the exact candle where the price comes back to the average instead of on every candle near it.
- Entries and exits are separate branches: two position modify blocks open with the order volume, and two more only close what is held.

## Entry and Exit Rules

- **Long entry**: The fast EMA is above the slow one, the close crosses back down onto the fast EMA and the position is not long. The order buys Volume plus the absolute value of the current position, which opens a long from flat or turns a short straight into a long.
- **Short entry**: The fast EMA is below the slow one, the close crosses back up onto the fast EMA and the position is not short. The order sells Volume plus the absolute value of the current position, which opens a short from flat or turns a long straight into a short.
- **Exit**: A long is closed when the fast EMA drops below the slow one, and a short when the fast EMA climbs above it; both closing blocks work on the whole open position, so a repeated signal on a flat book does nothing. There is no protective stop, which is how the original strategy is written.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast EMA length | 8 | Period of the fast exponential moving average, the one the price pulls back to. |
| Slow EMA length | 21 | Period of the slow exponential moving average, the one that sets the trend direction. |
| Volume | 1 | Base order volume, in lots; on a reversal the open position is added to it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both moving averages and a converter that reads the close price.
- The crossing block takes the fast EMA on its up input and the close on its down input, so its true output is the close falling back onto the average and a logical NOT of it is the close rising back onto it.
- Two comparison blocks put the averages against each other, and four more compare the position against a shared zero constant, which gives both the entry guards and the exit guards.
- The entry branch takes its volume from a formula that adds the absolute position to the volume constant, while the two closing blocks are set to close the position and need no volume at all.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
