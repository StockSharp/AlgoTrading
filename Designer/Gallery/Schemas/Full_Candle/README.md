# Full Candle Momentum Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A full candle is one that opens at one end of its range and closes at the other: the shadows together take up no more than a small share of the high-to-low distance. Such a bar is a single uninterrupted push, and the diagram joins it in the direction of the body as long as an exponential moving average agrees with that direction. The trade is given a fixed target of a fraction of a percent and nothing else.

![schema](schema.svg)

## Strategy Overview

- Converters read the open, high, low and close of the finished candle, and two formula blocks measure how much of the range the shadows take.
- The bullish measure is the upper shadow plus the lower shadow of a rising candle, scaled by a hundred and compared with the shadow share applied to the full range; the bearish measure is its mirror.
- An exponential moving average of the closing price is the trend filter: full bullish candles are only bought above it, full bearish candles only sold below it.
- A position protection block closes every trade at a fixed take profit, which is the only exit the original strategy has.

## Entry and Exit Rules

- **Long entry**: The bullish shadow measure is below zero, which means the candle rose and its shadows stayed within the allowed share of the range, the close is above the EMA and the position is not already long. The order buys the volume constant plus whatever short is open, so it reverses a short and opens a long in one order.
- **Short entry**: The bearish shadow measure is below zero, the close is below the EMA and the position is not already short. The order sells the volume constant plus whatever long is open, reversing a long and opening a short in one order.
- **Exit**: The position protection block takes profit at 0.3 percent from the entry price, the same figure the original strategy hard-codes, and there is no stop loss because the original has none. Two differences are worth knowing. The protection block watches the price inside the bar, while the original checks only the close of a finished candle, so the target is hit slightly earlier here. And the original's pause of fifteen candles after every trade is left out: a bar counter cannot be assembled without feeding a signal back into the diagram, which would close the graph into a loop, so a reversal signal is taken as soon as it appears.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| EMA Length | 20 | Averaging length of the exponential moving average used as the trend filter. |
| Shadow share, % | 10 | Largest share of the candle's high-to-low range, in percent, that both shadows together may take. |
| Take profit, % | 0.3 | Take profit distance from the entry price, in percent. |
| Volume | 1 | Order volume, in lots; the reversal order adds the size of the position being closed on top of it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. The original strategy runs on fifteen-minute candles; five minutes is used here so that the pattern appears often enough on the packaged history. |

## Diagram Details

- Each formula subtracts the allowed shadow budget from the actual shadows, so a value below zero means the candle is full-bodied; the constant with the shadow share feeds both formulas.
- The direction does not need a comparison of its own: written for a rising candle, the bullish measure is always positive on a falling one and on a candle with no range at all, so a value below zero already means the candle rose.
- The position block goes two ways: into the comparisons against zero that guard the entries, and into the volume formula, which adds the absolute position to the volume constant so that one market order closes the opposite side and opens the new one.
- Both entry blocks pass their own trades to the position protection block, which registers the take profit; the closing price is fed to the same block as its price reference.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
