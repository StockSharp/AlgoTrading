# Twenty Pips Once a Day Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram takes at most one counter-trend position per day. Once a day, at a chosen hour of the clock and only while the account is flat, it compares the close of the finished hourly candle with the close of the candle 29 bars earlier and bets against the drift of that window: it buys after a fall and sells after a rise. A small take-profit, a wider stop and a hard age limit on the position take care of the exit.

![schema](schema.svg)

## Strategy Overview

- Finished hourly candles drive everything. Nothing is evaluated inside a forming bar, so every decision is taken on a closed price.
- Previous value holds the candle from 29 bars back. Its close is compared with the current close, which measures the drift of roughly the last day and a quarter.
- The comparison decides the side against that drift: an older close above the current one means the market fell and the schema buys; an older close below it means the market rose and the schema sells. Two strict comparisons are used, so a window that ends exactly where it began produces no signal at all.
- Time supplies the clock reading that goes with the candle just closed, a converter takes its hour, and a comparison against the Trading Hour parameter opens the entry window for one candle a day.
- The current position must be flat. Together with the once-a-day hour filter and the Open position condition on the entry blocks, this is what keeps the schema to a single position at a time.
- Both entries are market orders of a fixed volume. Their fills are merged and handed to Position protection, which closes the position at a 0.1% take-profit or a 0.5% stop-loss, the same one-to-five ratio the idea is built on.
- An N values counter is armed by the accepted entry and counts 21 finished candles. When it runs out, a Position modify block set to Close position flattens whatever is still open, so a position that neither target reached is not carried indefinitely.
- Is trade allowed watches the platform's live trading permission. On each accepted entry the schema records what that permission was at that moment and writes one line to the log, which is a report rather than a veto: in a historical replay the permission is never granted, so gating the entry on it would silence the whole diagram.

## Entry and Exit Rules

- **Long entry**: On a finished hourly candle whose clock hour equals Trading Hour, with the position flat and the close of 29 bars ago above the current close, buy Volume at market under the Open position condition.
- **Short entry**: On a finished hourly candle whose clock hour equals Trading Hour, with the position flat and the close of 29 bars ago below the current close, sell Volume at market under the Open position condition.
- **Exit**: Position protection closes the position at a 0.1% take-profit or a 0.5% stop-loss measured from the entry fill, with the candle close feeding its price checks. If neither level is reached, the N values counter fires 21 finished candles after the entry and the Close position block flattens the remainder; when protection has already closed the position, that action finds nothing to close and does nothing.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 01:00:00 | Time frame of the working candles. Only finished candles are processed, so an order can never be dated inside a bar that is still forming. |
| Lookback Bars | 29 | How many bars back the reference close is taken from. This is the width of the window whose drift the entry fades. |
| Trading Hour | 7 | Hour of the clock at which the daily entry window opens, read from the strategy time that accompanies the finished candle. |
| Volume | 0.1 | Fixed quantity of both entry orders. There is no adaptive sizing: every entry is the same size. |
| Max Position Bars | 21 | How many finished candles a position may live before it is flattened regardless of profit or loss. |
| Take Profit % | 0.1 | Favourable move at which Position protection closes the position, as a percentage of the entry price. |
| Stop Loss % | 0.5 | Adverse move at which Position protection closes the position, as a percentage of the entry price. The stop is fixed, not trailing. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block is set to finished candles only, and [Previous value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) is placed on the candle itself rather than on a price, with a [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) after it. Two [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks turn the two closes into the long and the short side; because both are strict, an unchanged window produces neither.
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) is a data source here, not a label: a converter reads its Hour and a comparison matches it against a [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html). The hour and the flat-position check are both anchored to the candle, because the constants they are compared against are triggered by the candle stream, so the entry gate can only complete once per finished bar.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) and a comparison against zero supply the flat check, and the two [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) blocks gather drift, hour and position into one signal per side. Both [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks carry the Open position condition, which is the second guard against a repeated entry while a position is open.
- The accepted signal also arms [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html), which counts finished candles and then triggers a third Position modify block set to Close position. That block takes no volume: the amount to close is derived from the open position, and a flat account simply yields no order.
- [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) merges the fills of both entry sides for [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html). In parallel, a [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) releases exactly one pulse per entry and is reset by the age counter; that pulse latches the reading of [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) into a variable, which a [String format](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) block turns into one [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) log line per position taken.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
