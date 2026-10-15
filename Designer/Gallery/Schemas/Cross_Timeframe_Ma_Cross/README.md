# Cross-Timeframe Moving Average Crossover Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram combines a 10-period simple moving average from finished four-hour candles with a 40-period simple moving average from finished one-hour candles. Both settings represent a nominal 40-hour lookback. The latest formed higher-timeframe average is retained and paired with the current base average once per finished base candle; crossover direction and current position route fixed 0.1 market actions, including fill-confirmed two-step reversals. The chart shows one-hour candles, both synchronized averages, and four execution streams.

![schema](schema.svg)

## Strategy Overview

- Finished four-hour candles feed Higher SMA 10, while finished one-hour candles feed Base SMA 40 and drive the decision cycle. With the default settings, each average covers 40 nominal hours: 10 × 4 hours and 40 × 1 hour.
- The latest formed Higher SMA value is retained. On every finished one-hour candle, the diagram refreshes that retained value and the current Base SMA, then a Sync block with a one-hour interval and the base candle as its anchor releases the aligned values together.
- One Crossing block emits `true` when Higher SMA crosses above Base SMA and `false` when it crosses below. A NOT block converts the downward event into a positive trigger for the short path.
- The current position separates each crossover into flat, long, and short cases. A flat account opens 0.1 in the signal direction, an already aligned position does nothing, and an opposite position enters a staged reversal.
- A staged reversal first submits a ReduceOnly market action for 0.1. Only the fully matched close Order triggers the fixed NoCondition market action for 0.1 in the new direction. This sequence is sized for exposure created by the diagram with the same Order Volume; a different actual position size may not finish at the target exposure. There is no stop-loss or take-profit block.

## Entry and Exit Rules

- **Long entry**: When Crossing emits an upward event, the flat-position branch submits a NoCondition market buy for Order Volume 0.1. A short position first submits a ReduceOnly market buy for 0.1; only its fully matched Order triggers the second NoCondition buy for 0.1. An existing long position is left unchanged.
- **Short entry**: When Crossing emits a downward event, NOT activates the short path. The flat-position branch submits a NoCondition market sell for Order Volume 0.1. A long position first submits a ReduceOnly market sell for 0.1; only its fully matched Order triggers the second NoCondition sell for 0.1. An existing short position is left unchanged.
- **Exit**: There is no independent exit, stop-loss, or take-profit rule. A qualified crossover in the opposite direction runs the fixed-volume close-and-open sequence. Its ReduceOnly first leg cannot increase or reverse exposure, but the following NoCondition action is not resized to an external position; if the actual size differs from Order Volume, the sequence does not guarantee a final position of the target size.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Higher Candles Series | 04:00:00 | Four-hour candle series used by Higher SMA. Only finished candles update the retained higher-timeframe average. |
| Base Candles Series | 01:00:00 | One-hour candle series used by Base SMA. Each finished candle anchors one synchronized evaluation and is also drawn on the chart. |
| Higher SMA Length | 10 | Period of the simple moving average calculated on four-hour candles. Ten bars represent a nominal 40-hour lookback. |
| Higher SMA Source | unset | Left unset, so Higher SMA reads the Close price of each finished four-hour candle. |
| Base SMA Length | 40 | Period of the simple moving average calculated on one-hour candles. Forty bars represent the same nominal 40-hour lookback. |
| Base SMA Source | unset | Left unset, so Base SMA reads the Close price of each finished one-hour candle. |
| Order Volume | 0.1 | Fixed quantity used for flat entries, ReduceOnly close actions, and the fill-confirmed second leg of a reversal. |

## Diagram Details

- Two [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) blocks emit only finished four-hour and one-hour candles. Separate formed-only [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate SimpleMovingAverage 10 and SimpleMovingAverage 40 from their Close prices.
- A [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) retains the latest formed Higher SMA. Each finished base candle refreshes that value and Base SMA before entering [Sync](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/sync.html), whose Interval is `01:00:00`, ClearSockets is enabled, and candle input provides the hourly anchor.
- The synchronized numeric outputs enter one [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) block. Its upward `true` event drives the long route, while a NOT [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) changes the downward `false` event into a positive short trigger.
- The current [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is refreshed in the same base-candle cycle. [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks distinguish `Position = 0`, `Position > 0`, and `Position < 0`, so an aligned position cannot receive another entry.
- For an upward event, the externally filtered flat route invokes a buy [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) block with NoCondition. The short route first invokes a buy block with ReduceOnly; its Order output is raised only after the close order is fully matched and then stages the fixed NoCondition buy.
- The downward route is symmetric: the externally filtered flat route opens a short with NoCondition, while a long position is reduced by a sell action before its fully matched Order stages the fixed NoCondition sell. All four actions use MarketOrder and Order Volume 0.1. No stop-loss, take-profit, or timed-exit block is present.
- The [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) receives finished one-hour candles, the synchronized Higher SMA and Base SMA values, and the MyTrade outputs of the flat-long, flat-short, close-short, and close-long actions.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
