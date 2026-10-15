# Moving Average Crossover Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The oldest trend diagram there is: a fast exponential moving average against a slow one, with the position reversed each time they cross. A protective block adds what the crossing alone cannot give, a percent stop that closes the position when the move goes against it.

![schema](schema.svg)

## Strategy Overview

- Two exponential moving averages, one fast and one slow, are calculated on finished candles of a single instrument.
- A crossing block fires only on the bar where the fast average actually crosses the slow one, and its direction tells long from short.
- The position protection block watches the close of every finished candle and closes the position once it is a given percent away from the entry price.

## Entry and Exit Rules

- **Long entry**: The fast EMA crosses above the slow one and the position is not already long. The order buys Volume plus the absolute value of the current position, which opens a long from flat or turns a short straight into a long.
- **Short entry**: The fast EMA crosses below the slow one and the position is not already short. The order sells Volume plus the absolute value of the current position, which opens a short from flat or turns a long straight into a short.
- **Exit**: Either the opposite crossing reverses the position with a single order, or the protective stop closes it when the candle close is a given percent worse than the average entry price.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast EMA length | 20 | Period of the fast exponential moving average. |
| Slow EMA length | 80 | Period of the slow exponential moving average. |
| Stop loss, % | 2 | Distance of the protective stop from the entry price, in percent. |
| Volume | 1 | Base order volume, in lots; the open position is added to it on a reversal. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both indicator blocks, and their outputs meet in the crossing block.
- The crossing output is the long signal; a logical NOT of it is the short signal, and each is joined by a logical AND with a comparison of the position against zero.
- A formula block computes Volume plus the absolute position and feeds the volume input of both position modify blocks, so one market order can reverse the position.
- Both position modify blocks send their own trades into the protection block, and a converter takes the close price of each finished candle into its price input, so the stop is checked on candle closes.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
