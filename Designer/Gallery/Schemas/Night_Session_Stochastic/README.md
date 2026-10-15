# Night Session Stochastic Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A stochastic oscillator on four-hour candles that is allowed to act only at night. The interesting part is the clock, not the oscillator: a night session starts in the evening and ends the next morning, so its start is a later time of day than its end, and a single Working time block cannot describe an interval like that. The diagram therefore builds the night out of two halves - one before midnight, one after - and joins them into a single answer before anything else is allowed to look at it.

![schema](schema.svg)

## Strategy Overview

- Finished four-hour candles drive the whole diagram, so every decision is taken on a closed bar and nothing reacts to a bar still being built.
- A Stochastic oscillator with a 14-bar %K and a 3-bar %D runs on those candles, and a Converter picks the %K line out of its value; %D is only drawn, never traded on.
- Two Working time blocks read the moment each candle opens: one covers 21:00 to 23:59:59, the other 00:00 to 06:00. Neither of them alone is the night.
- A Logical condition set to Exclusive or joins the two halves into one night signal. The halves cannot overlap, so exactly one of them can be open at a time, and the block answers once per candle with both halves in hand.
- Two Comparison blocks place %K against the oversold and overbought levels; two more place the position against zero, which tells the diagram whether it is flat, long or short.
- Four Logical condition And gates combine those three facts - night, oscillator, position - into two entries and two exits, so no gate can fire unless the clock agrees with it.
- Entries are Position modify blocks running under the Open position condition: a market order for Order Volume leaves only while the position is exactly zero, which is what keeps one signal from becoming a stream of orders.
- The chart panel draws the candles, the oscillator and every order and fill the diagram produces, so the night windows can be read straight off the picture.

## Entry and Exit Rules

- **Long entry**: While the night is open, the position is flat and %K is below the oversold level, the long gate fires and Position modify buys Order Volume at market. The Open position condition on that block means a repeat of the same reading changes nothing until the position is closed again.
- **Short entry**: The mirror image: night open, position flat and %K above the overbought level make the short gate fire, and Position modify sells Order Volume at market under the same Open position condition.
- **Exit**: There is no take-profit, no stop-loss and no timed flatten. A long is closed by the opposite extreme - %K above the overbought level while the position is long - and a short by %K below the oversold level while the position is short, both through a Position modify block set to Close position, which reads the open quantity itself. The exits are inside the night window too, so a position opened at night is carried through the day and released the next night. Closing and reversing are two separate events: the extreme that closes a long only flattens it, and a later reading of the same kind is what opens the short.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 04:00:00 | Four-hour time frame, built from the smaller candles available in the data. Only finished candles are processed, and each of them is timed by its opening moment, which is what decides the session it belongs to. |
| %K Length | 14 | Look-back of the %K line: how many candles the oscillator measures the close against. |
| %D Length | 3 | Smoothing length of the %D line. It is drawn on the panel and is not part of any condition. |
| Evening Half From | 21:00:00 | Start of the half of the night that lies before midnight. Keep the two halves apart: they are meant to meet at midnight, not to overlap. |
| Evening Half Until | 23:59:59 | End of the evening half. One second before midnight closes it without letting it touch the half that follows. |
| Morning Half From | 00:00:00 | Start of the half of the night that lies after midnight, which is midnight itself. |
| Morning Half Until | 06:00:00 | End of the morning half, and with it the end of the night. After this hour no gate in the diagram can fire until the evening half opens again. |
| Oversold Level | 30 | Level below which %K is treated as oversold: it opens a long when the position is flat and closes a short when one is open. |
| Overbought Level | 70 | Level above which %K is treated as overbought: it opens a short when the position is flat and closes a long when one is open. |
| Order Volume | 1 | Quantity sent by both entries. The exits ignore it and close whatever is open. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block subscribes to a four-hour series with only-formed candles switched on, and it stamps every value it sends with the moment the candle opened. That stamp is what the clock blocks read, so a candle belongs to the session its opening hour falls in, whatever happens during the four hours that follow.
- [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) holds the Stochastic oscillator and passes values on only once it is formed, so the first bars of history arm the calculation without producing signals. A [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) reads the %K field out of the oscillator's value and hands a plain number to the comparisons.
- The two [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) blocks are the point of the diagram. Each one is true while the time of day sits between its own two bounds, which means a block can never describe a window that runs past midnight: its start would be later than its end and the check could never pass. Splitting the night at midnight gives two ordinary windows, and the [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) above them puts the night back together.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) is compared against a zero [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) by three [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks at once - equal, greater, less - and those three answers are what separate the two entry gates from the two exit gates. The oversold and overbought levels and the order volume are Variables as well, so every number the diagram argues about is a parameter and not a value buried in a block.
- Four [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks act on the gates. The two entries carry the Open position condition and take their quantity from the volume Variable; the two exits carry the Close position condition and need no quantity, because a closing order is sized from the position it undoes. Their orders and fills go to the [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) alongside the candles and the oscillator.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
