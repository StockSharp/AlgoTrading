# Simple Multiple Time Frame Moving Average Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The name promises two time frames, but the C# strategy it comes from subscribes to one four-hour series and reads two ExponentialMovingAverage lines of different lengths on it. What is really traded is the agreement of their slopes: while both the short and the long average point up the diagram is long, while both point down it is short, and a disagreement leaves the position alone.

![schema](schema.svg)

## Strategy Overview

- Two ExponentialMovingAverage blocks, a short one and a long one, work on the same candle series; the diagram keeps that single subscription instead of inventing a second time frame.
- The slope of each average is read by comparing its current value with a previous-value block set one candle back, so a rising average is simply one that stands above where it stood before.
- Every order uses the fixed shared volume, so an opposite signal only flattens the position; opening the other way takes a second signal of the same direction on the following candle, exactly the behaviour of the source code.
- The condition is a state, not an event: it is re-checked on every finished candle, which is why comparisons and logical ANDs are used here and no crossing block is needed.

## Entry and Exit Rules

- **Long entry**: The fast ExponentialMovingAverage stands above its own value one candle back, the slow one does the same, and the position is not already long. The modify block buys the shared volume at market, which opens a long from flat or closes an existing short.
- **Short entry**: The fast ExponentialMovingAverage stands below its own value one candle back, the slow one does the same, and the position is not already short. The modify block sells the shared volume at market, which opens a short from flat or closes an existing long.
- **Exit**: There is no exit rule of its own: the position is closed by the opposite signal, that is, when both averages have turned the other way. The source strategy carries no stop loss, no take profit and no pause between trades, and neither does this diagram.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast EMA length | 5 | Length of the fast ExponentialMovingAverage. |
| Slow EMA length | 20 | Length of the slow ExponentialMovingAverage. |
| Volume | 1 | Order volume, in lots; the same constant feeds both modify blocks. |
| Candles | 04:00:00 | Candle time frame the whole diagram works on; the original uses four hours and this is kept, which leaves about two hundred candles on the packaged month of history. |

## Diagram Details

- The candle block feeds both indicator blocks, and each indicator feeds a previous-value block typed as an indicator value.
- Four comparison blocks turn the two averages and their delayed copies into rising and falling flags.
- The position block, compared with a zero constant twice, supplies the guard that keeps an entry from adding to a position it already holds.
- Each logical AND joins one fast condition, one slow condition and one position condition, and fires a position modify block that takes its size from the shared volume constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
