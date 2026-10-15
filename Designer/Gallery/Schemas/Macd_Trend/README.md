# MACD Trend Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The diagram follows the trend with MACD: the difference between a fast and a slow exponential moving average is smoothed once more into a signal line, and every crossing of the two lines turns the position around. The order volume carries the open position with it, so one order closes what is held and opens the opposite side.

![schema](schema.svg)

## Strategy Overview

- MACD is assembled from its parts on the diagram: EMA(12) minus EMA(26) is the MACD line, and an EMA(9) of that line is the signal line, which keeps all three periods available as schema parameters.
- A crossing block compares the two lines and fires only on the bar where they actually cross, upwards or downwards.
- The strategy is always in the market after the first signal; there is no separate exit, the opposite crossing reverses the position.

## Entry and Exit Rules

- **Long entry**: The MACD line crosses above the signal line and the position is not already long. The order buys Volume plus the absolute value of the current position, which opens a long from flat or turns a short straight into a long.
- **Short entry**: The MACD line crosses below the signal line and the position is not already short. The order sells Volume plus the absolute value of the current position, which opens a short from flat or turns a long straight into a short.
- **Exit**: There is no dedicated exit block and no protective stop: a position is left only by the opposite crossing, which reverses it in a single order.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast EMA length | 12 | Period of the fast exponential moving average inside MACD. |
| Slow EMA length | 26 | Period of the slow exponential moving average inside MACD. |
| Signal EMA length | 9 | Smoothing period of the signal line built on the MACD line. |
| Volume | 1 | Base order volume, in lots; the open position is added to it on a reversal. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both moving averages; a formula block subtracts the slow one from the fast one and produces the MACD line.
- The MACD line goes on into a third indicator block, an EMA(9), which is the signal line, and both lines meet in the crossing block.
- The crossing output is the long signal; a logical NOT of it is the short signal, and each is joined by a logical AND with a comparison of the position against zero.
- A second formula block computes Volume plus the absolute position and feeds the volume input of both position modify blocks, which is how one market order reverses the position.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
