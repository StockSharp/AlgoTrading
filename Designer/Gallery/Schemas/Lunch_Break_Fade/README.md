# Lunch Break Fade Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram fades short-term price movement during the 11:00:00–14:59:59 lunch window on finished five-minute candles. It enters only from a flat position, exits by comparing the close with a formed 20-period SMA, and blocks every entry and exit path for the next 30 completed candles after an order signal.

![schema](schema.svg)

## Strategy Overview

- Only finished five-minute candles enter the decision chain. The SMA emits after its 20-period warm-up, and two Previous value blocks provide the immediately preceding two closes.
- The Working time block reads each candle's opening time and permits entries from 11:00:00 through 14:59:59. The time window does not restrict exits.
- A rising pair of previous closes followed by a bearish current candle produces a short entry from a flat position. A falling pair followed by a bullish candle produces a long entry from flat.
- A long position exits when the close is below the SMA, and a short position exits when the close is above the SMA. Four separate paths submit fixed-volume market orders for the two entries and two exits.
- Every entry or exit signal activates a cooldown that blocks both kinds of action for the next 30 completed candles. There is no position-protection block; the chart displays candles, the SMA, and fills from all four order paths.

## Entry and Exit Rules

- **Long entry**: During the lunch window, when `Close[-1] < Close[-2]`, the current candle is bullish (`Close > Open`), the position snapshot is zero, and the cooldown is ready, the diagram submits a market buy for Volume 1.
- **Short entry**: During the lunch window, when `Close[-1] > Close[-2]`, the current candle is bearish (`Close < Open`), the position snapshot is zero, and the cooldown is ready, the diagram submits a market sell for Volume 1.
- **Exit**: With the cooldown ready, a long position sends a market sell when `Close < SMA`, while a short position sends a market buy when `Close > SMA`. These level checks run both inside and outside the lunch window. No stop-loss, take-profit, or other protection is attached.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Five-minute time frame; only finished candles drive the indicator, history, cooldown, and decisions. |
| SMA Period | 20 | Period of the SimpleMovingAverage used by both exit level checks. |
| Cooldown Bars | 30 | Number of subsequent completed candles during which entry and exit signals are blocked. |
| Lunch Begin | 11:00:00 | Inclusive opening-time boundary at which lunch entries become eligible. |
| Lunch End | 14:59:59 | Inclusive opening-time boundary through which lunch entries remain eligible. |
| Volume | 1 | Fixed quantity supplied to all four NoCondition market-order blocks. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished five-minute candles. A formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calculates SimpleMovingAverage 20, and a [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) exposes its numeric value. The [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) gate releases the stored candle into the decision chain.
- Close and Open [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) blocks extract candle prices. Two [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) blocks use shifts 1 and 2; a history-ready gate prevents decisions until both preceding closes are available.
- The [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) block receives the candle stream directly and evaluates the opening-time metadata against the inclusive lunch boundaries. Its result participates only in the two entry conditions.
- The current [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is stored by a [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) and released once per decision candle. [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) and [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) blocks combine session, prior direction, candle direction, position, history, SMA level, and cooldown state.
- A [Delay Signal](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) block counts 30 later finished candles. Ready-state Variables suppress all four action conditions during the count and enable them again on the following candle.
- Four [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks place market orders with `NoCondition` and shared Volume 1: buy entry, sell entry, sell exit from long, and buy exit from short. No protection element is present.
- The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) receives finished candles, the formed SMA stream, and the MyTrade output from each of the four order blocks.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
