# Overnight Session Flip Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram combines a formed 20-period SMA with two scheduled market-order windows. The strategy clock evaluates the latest finished five-minute candle, position, and calendar date: a qualifying buy can be sent during hour 20, while a qualifying sell can be sent during hour 8. A date latch limits the diagram to one submitted order per calendar date.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles update the close-price value and SimpleMovingAverage with a period of 20.
- The SMA block emits only formed values, so scheduled decisions wait until the indicator has enough candle history.
- The Time block is the decision clock. Each tick releases the latest close, SMA, and position snapshots, then supplies the hour and calendar components used by the gates.
- Both market-order blocks use a fixed volume of 1 with no position-modification condition. The position filters permit buys only at or below zero and sells only at or above zero.
- A numeric calendar key is captured before an order block is triggered. The chart displays candles, SMA values, and the two MyTrade streams; the diagram has no protection block or separate exit block.

## Entry and Exit Rules

- **Long entry**: During Night Hour 20, the latest finished-candle close is above the formed SMA, the current position is less than or equal to zero, and no order has been submitted on the current calendar date. The diagram submits a market buy for Volume 1.
- **Short entry**: During Day Hour 8, the latest finished-candle close is below the formed SMA, the current position is greater than or equal to zero, and no order has been submitted on the current calendar date. The diagram submits a market sell for Volume 1.
- **Exit**: There is no dedicated exit or protective order. A later qualifying fixed-size order in the opposite direction may reduce an existing position, close an equal opposite position, or cross through zero when the current magnitude is smaller than Volume; it does not guarantee a complete reversal.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Five-minute candle time frame; only finished candles update the price and SMA snapshots. |
| SMA Period | 20 | Number of finished candles used by SimpleMovingAverage; decisions require a formed SMA value. |
| Night Hour | 20 | Strategy-clock hour in which the buy conditions may submit an order. |
| Day Hour | 8 | Strategy-clock hour in which the sell conditions may submit an order. |
| Volume | 1 | Fixed quantity supplied to both market-order blocks. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) stream feeds a close-price [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) and a formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html). A [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) with expression `a` exposes the SMA as a numeric value.
- [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) supplies the strategy/message timestamp. On every clock tick it triggers [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) blocks holding the latest close, SMA, and position values, so Time participates directly in every decision.
- Time converters extract Hour, Year, and DayOfYear. The date [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) calculates `Year * 1000 + DayOfYear`, producing a stable key for each calendar date.
- [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks evaluate the two scheduled hours, close versus SMA, position versus zero, and the current date key versus the last captured key. Two [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) blocks combine the buy and sell gates.
- The current [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is sampled on each clock tick. The buy side requires `Position <= 0`, while the sell side requires `Position >= 0`.
- A true combined signal first captures the current date key and then triggers its [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) block. This ordering prevents another submitted order on later clock ticks of the same calendar date.
- Both Modify position blocks receive the shared fixed Volume value and place market orders. The Chart panel receives finished candles, the formed SMA stream, and MyTrade outputs from the buy and sell blocks.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
