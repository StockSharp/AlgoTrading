# EMA Cross Trade Alerts Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades upward and downward crossings of a 120-period fast EMA and a 450-period slow EMA on finished one-minute candles. A position snapshot gates each signal, fixed-volume market orders manage exposure, every own fill is written to the log, and the chart shows candles, both EMA streams, and buy and sell fills.

![schema](schema.svg)

## Strategy Overview

- Finished one-minute candles feed the fast EMA 120 and slow EMA 450. Formed-only filtering is disabled for both indicators, so their values are available from the beginning of the calculation.
- The Crossing block emits `true` for an upward fast-over-slow crossing. A NOT block converts its `false` downward-crossing event into the sell-side trigger.
- At each candle evaluation, the current position is released from a candle-triggered snapshot before the EMA signals are processed. Comparisons allow buying only when `Position <= 0` and selling only when `Position >= 0`.
- Both market-order blocks use `NoCondition` and a fixed Volume of 1. An opposite signal can reduce or flatten a position and can cross zero when its magnitude is below Volume, but it does not guarantee a complete reversal.
- The Strategy trades block sends every own fill through the exact fill-message template to a Log notification. The chart receives finished candles, both EMA streams, and the buy and sell fill streams.

## Entry and Exit Rules

- **Long entry**: When the fast EMA crosses above the slow EMA and the candle-time position snapshot is less than or equal to zero, the diagram submits a market buy for Volume 1.
- **Short entry**: When the fast EMA crosses below the slow EMA and the candle-time position snapshot is greater than or equal to zero, the diagram submits a market sell for Volume 1.
- **Exit**: There is no dedicated exit or protection block. A later qualifying fixed-volume order in the opposite direction may reduce the current position, flatten an equal-sized position, or cross through zero when the position is smaller than Volume; a complete reversal is not guaranteed.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:01:00 | One-minute candle time frame; only finished candles drive the EMA and decision chain. |
| Fast EMA Period | 120 | Period of the fast ExponentialMovingAverage; formed-only filtering is disabled. |
| Slow EMA Period | 450 | Period of the slow ExponentialMovingAverage; formed-only filtering is disabled. |
| Volume | 1 | Fixed quantity supplied to both NoCondition market-order blocks. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits only finished one-minute candles and drives the position snapshot before it feeds the EMA calculations.
- Two [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate ExponentialMovingAverage values with periods 120 and 450. Their formed-only option is `false`, and both outputs are also sent to the chart.
- The [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) output is `true` on an upward crossing and `false` on a downward crossing. A NOT [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) turns the downward event into a positive sell trigger; separate AND blocks combine direction and position.
- The current [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is stored continuously and released once per candle before the EMA path. [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks evaluate `Position <= 0` and `Position >= 0` in the same causal candle wave as the crossing.
- The buy and sell [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks place market orders with `NoCondition` and the shared fixed Volume value. No stop, take-profit, or separate exit block is present.
- [Strategy trades](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) emits every own `MyTrade`. The [String Formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) uses exactly `EMA cross fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}`.
- The [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) block writes each formatted fill with Type `Log` and Caption `EMA cross trade`. The chart plots the candles, fast EMA, slow EMA, and separate buy and sell fill streams.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
