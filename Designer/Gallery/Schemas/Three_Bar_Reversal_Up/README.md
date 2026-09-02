# Three-Bar Reversal Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two candles push the market down, the second one printing a lower low than the first, and then a third candle turns and closes above the high of the second. That sequence says the sellers spent their last push and were answered in full, and the diagram buys it. The mirror image of the shape is sold. A simple moving average of the closing price carries the trade afterwards and decides when it is over.

![schema](schema.svg)

## Strategy Overview

- Two candle pattern blocks each hold a three-candle formula, so the whole shape is recognized in one block instead of a wall of comparisons.
- The long formula asks for a bearish candle, then a bearish candle with a lower low, then a bullish candle closing above the middle candle's high.
- The short formula is the exact mirror: bullish, bullish with a higher high, then bearish closing below the middle candle's low.
- The simple moving average takes no part in the entry; it is only the line the trade is given up on, exactly as in the original strategy.

## Entry and Exit Rules

- **Long entry**: The up pattern block reports the completed three-candle reversal and the position is flat. The order buys one lot and opens a long.
- **Short entry**: The down pattern block reports the completed mirror reversal and the position is flat. The order sells one lot and opens a short.
- **Exit**: A long is closed once a candle closes below the moving average, a short once a candle closes above it, both through position modify blocks in close mode, which is exactly what the original does. The original also has neither a stop loss nor a take profit, so the diagram has none either. What is left out is the pause of several hundred candles the original keeps after every trade: a bar counter cannot be built out of blocks without feeding a signal back into the diagram, which would close the graph into a loop, so this example simply takes every pattern it sees. It therefore trades far more often than the original.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. The original strategy runs on one-minute candles; five minutes is used here to match the packaged history and keep the pattern readable. |

## Diagram Details

- The candle block feeds four branches: the two pattern blocks, the moving average and a converter that pulls the closing price out of the candle.
- Each pattern block carries three formulas, one per candle of the shape, and reports true only on the candle that completes it; the p-prefixed values inside a formula read the candle before it.
- The position block is compared against a zero constant and that single guard protects both entries, so one pattern gives one trade.
- Both entry blocks send market orders and take their volume from one shared constant; the two exit blocks are triggered straight from the moving average comparisons and act only when there is something to close.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
