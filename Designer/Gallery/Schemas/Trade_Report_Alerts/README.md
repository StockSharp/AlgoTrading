# Trade Report Alerts Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram is an example of reporting rather than of signal invention. A plain crossing of a 9-period and a 26-period exponential moving average on finished five-minute candles supplies the trades, and everything around it turns those trades into readable text: every own fill becomes one log line the moment it happens, and once a day a clock-driven branch writes out the realized result of the strategy. The reporting side reads the trading side and never places, changes or blocks an order of its own.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed a fast ExponentialMovingAverage of 9 and a slow one of 26. The Crossing block reduces the pair to a single event: `true` when the fast line crosses above the slow one, `false` when it crosses below, and nothing at all in between.
- The position is read once per candle through a candle-triggered snapshot, and three comparisons against zero describe it as flat, long or short. Every decision is built from that snapshot, so a fill arriving in the middle of a bar cannot re-open a decision that was already taken.
- From a flat position an upward crossing opens a long and a downward crossing opens a short. Both entry blocks carry the Open-position condition, so they stay silent for as long as any position is held and cannot pile order on order.
- From a held position the opposite crossing closes it through a Close-position block, which sizes the order from the position itself. The order event of that close then triggers the entry in the new direction, so a reversal is written as two explicit steps instead of one oversized order.
- One exposed Volume value feeds all four entry blocks; the two closing blocks take no volume, because a Close-position block already knows how much is open.
- The Strategy trades block picks up every own fill of the strategy and sends it through a String formatter into a Log notification, so each execution leaves one line carrying side, quantity, instrument and price.
- A second branch reports on the clock instead of on the market. Current time feeds two Working time windows -- a reporting window around midday and a reset window just after midnight -- and a Flag placed between them converts the whole reporting window into exactly one pulse per day.
- That single pulse releases the realized result of the strategy from a variable that holds the last value it was given, formats it and writes it to the log. Because the variable starts at zero, a status line still appears on a day that produced no fill at all.

## Entry and Exit Rules

- **Long entry**: An upward crossing of the fast exponential average over the slow one, evaluated while the candle-time snapshot shows a flat position, sends a market buy for Volume. If a short position is open instead, the same crossing first closes it in full, and the resulting close order immediately triggers the long entry, so the direction changes within the same candle.
- **Short entry**: A downward crossing of the fast exponential average below the slow one, evaluated while the candle-time snapshot shows a flat position, sends a market sell for Volume. If a long position is open instead, the same crossing first closes it in full, and the resulting close order immediately triggers the short entry.
- **Exit**: There is no stop, take-profit or protection block: a position is carried until the opposite crossing, which closes it completely through a Close-position block whose volume is derived from the position. The reporting branch observes fills and profit and never issues, replaces or cancels an order, so switching the notifications off would leave the trading behaviour unchanged.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series; only finished candles drive the indicators and every decision built on them. |
| Fast EMA Length | 9 | Period of the fast ExponentialMovingAverage; it is the faster half of the crossing pair. |
| Slow EMA Length | 26 | Period of the slow ExponentialMovingAverage; it is the slower half of the crossing pair. |
| Volume | 1 | Quantity supplied to the four entry blocks. The two closing blocks ignore it and take their size from the open position. |
| Report Window Begin | 12:00:00 | Start of the daily reporting window. The first moment inside it raises the status report. |
| Report Window End | 12:05:00 | End of the daily reporting window. It only has to be wide enough for the clock to fall inside it once; the flag keeps the report to a single line whatever the width. |
| Day Reset Begin | 00:00:00 | Start of the reset window that clears the flag and allows a new report on the following day. |
| Day Reset End | 00:05:00 | End of the reset window. Between this moment and the start of the reporting window the branch stays quiet. |
| Fill Report Caption | Trade report | Caption written on each fill notification, which is how the per-trade lines are recognised in the log. |
| Status Report Caption | Strategy status | Caption written on the daily status notification, which separates it from the per-trade lines. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished five-minute candles only, and two [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) blocks calculate ExponentialMovingAverage values of 9 and 26 over them. Formed-only filtering is off, so both lines are available from the start of the replay, and both are drawn on the chart.
- The [Crossing](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) block fires only at a crossing; a NOT [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) turns its downward event into a positive trigger. The current [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is stored in a [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) released once per candle, and three [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks turn it into flat, long and short flags that four AND conditions combine with the crossing.
- Six [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks act on those four conditions: two entries from flat with condition `OpenPosition`, two closes with condition `ClosePosition`, and two further `OpenPosition` entries triggered by the order event of the matching close, which is what makes a reversal happen in two steps. All six place market orders and none of them waits for an online connection.
- [Strategy trades](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) emits every own fill. A [String Formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) renders it with the template `Fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}`, and a [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) of type `Log` writes it under the fill report caption.
- [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) feeds two [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) checks; the report window sets a [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) and the reset window clears it, which is what limits the branch to one pulse per day. The pulse releases the realized value of the [P&L](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) block from a variable preset to zero, a second String Formatter writes `Daily status: realized result {0}`, and a second `Log` notification publishes it.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
