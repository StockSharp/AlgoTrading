# Quote Momentum Sync Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Momentum measured from one close to the next. The diagram compares the close of the candle that has just finished with the close before it and asks for a move of at least a fixed number of price units, in the direction the previous candle was already going. Every reading is taken on the same five-minute beat, so the reference price, the direction filter and the position check are always in step and a signal is never assembled from values that belong to different moments.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles of the traded instrument are the only market data the diagram subscribes to, and everything downstream runs on that beat.
- Previous value holds the whole preceding candle rather than a single number, and two converters read its close and its open out of it.
- That previous close is the reference price: one formula adds the momentum step to it and another subtracts the step, which gives a long trigger level and a short trigger level for the current bar.
- A third converter reads the close of the candle that has just finished, and two comparisons place it against the two levels.
- The direction filter is the shape of the preceding candle: its close against its own open, so a bullish bar allows only longs and a bearish bar only shorts.
- A Position block compared with zero says whether the account is flat, which keeps the diagram out of an existing trade instead of adding to it.
- Two logical conditions collect direction, momentum and flatness, and each drives a Position modify that opens by market for a fixed volume and only from a flat position.
- Position protection takes the entry fills and the current close and closes the trade at a fixed percentage take-profit or stop-loss; the diagram carries no other exit.

## Entry and Exit Rules

- **Long entry**: The preceding candle closed above its own open, the candle that has just finished closed above the preceding close plus the momentum step, and the position is flat. Position modify buys the order volume at market.
- **Short entry**: The preceding candle closed below its own open, the candle that has just finished closed below the preceding close minus the same step, and the position is flat. Position modify sells the order volume at market.
- **Exit**: There is no exit signal in the diagram. From the moment an entry is filled the trade belongs to Position protection, which closes it at 0.5% profit or 0.5% loss from the entry price. Protection is priced from the candle close, so the levels are checked once per bar, and an opposite reading is ignored while a position is open: the next entry waits until the position is flat again.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series the whole diagram runs on. |
| Momentum Step | 5 | Distance from the preceding close, in price units, that the finished candle has to clear before an entry is allowed. |
| Order Volume | 0.01 | Order size, in lots. |
| Take Profit, % | 0.5 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 0.5 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- Previous value is set to hold a candle, not a number, and the fields are read afterwards by converters; that is what makes the reference price and the direction filter come from one and the same bar.
- The entry condition is written as two price levels rather than as a difference against a threshold, which is the same arithmetic seen from the other side and lets the chart show the level that produced each entry.
- Both entry blocks open only from a flat position, so repeated signals inside a live trade cost nothing and no order is sent to reverse or to pyramid.
- Every constant is triggered by the candle series, so it publishes its value on each bar; a constant that never fires would leave its comparison without a second operand and the condition would never assemble.
- The momentum step is an absolute distance in the price's own units, not a percentage, so it has to be rescaled for an instrument quoted in a different order of magnitude, while take-profit and stop-loss are percentages and carry over unchanged.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
