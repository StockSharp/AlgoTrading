# Gap Fill Reversal Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The diagram measures the jump between the close of one candle and the open of the next, and then waits for that candle to close back in the opposite direction. A gap down followed by a bullish candle is bought, a gap up followed by a bearish candle is sold, and a SimpleMovingAverage decides when the trade is over.

![schema](schema.svg)

## Strategy Overview

- The gap is expressed in percent of the previous close, so the same threshold keeps its meaning at any price level.
- A gap alone is not a signal: the candle that opened away from the previous close has to close back towards it, which is the reversal body the strategy is named after.
- SimpleMovingAverage is the only exit line, used by both sides; there is no stop loss and no take profit, exactly as in the original code.
- The diagram runs on one-minute candles, like the strategy it was taken from, so a gap here is the small discontinuity between two neighbouring minutes rather than an overnight gap.

## Entry and Exit Rules

- **Long entry**: The distance between the open and the previous close is at least Min Gap %, the open is below the previous close, the candle closes above its own open, and the position is flat. The order buys one lot at market.
- **Short entry**: The distance between the open and the previous close is at least Min Gap %, the open is above the previous close, the candle closes below its own open, and the position is flat. The order sells one lot at market.
- **Exit**: A long is given back on the first candle that closes below SimpleMovingAverage, a short on the first candle that closes above it; both closing blocks work the volume out from the open position themselves.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Min Gap % | 0.02 | Minimum distance between the previous close and the new open, in percent of the previous close. |
| SMA Length | 20 | Averaging length of the SimpleMovingAverage that closes the position. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:01:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- Two converter blocks read the open and the close of the candle, and a previous-value block keeps the close of the candle before it.
- The formula block turns the distance between the open and the previous close into a percentage, and one comparison holds that percentage against the threshold constant.
- Four further comparisons give the side of the gap and the side of the body; each logical AND joins a gap condition, a body condition and the flat-position check before the order block.
- The exit pair compares the close with the moving average and drives two close-position blocks. The 500-bar pause between trades of the original strategy has no counterpart among the blocks and is left out, so this diagram trades more often than the code does.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
