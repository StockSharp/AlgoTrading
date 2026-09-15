# Monday Weakness Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram trades a fixed weekly calendar. Each side of the week has its own day: on the short day it sells when the close sits below SMA 20, and on the cover day it buys that short back; on the long day it buys when the close sits above SMA 20, and on the exit day it sells that long out. The day of the week is read straight off the candle as a number, so the four calendar rules are four ordinary comparisons, and a clock-driven entry window keeps the weekly decision inside the active part of the day.

![schema](schema.svg)

## Strategy Overview

- Five-minute candles are delivered finished only. Two Converters read the same candle: one takes the close price, the other takes the opening time's day of the week, which arrives as a number where Sunday is 0 and Saturday is 6.
- Four Variables hold the four calendar days - short, cover, long and exit - and four Comparison blocks set to Equal turn the day number into four signals. Exactly one of them can be true on any given candle, which is what keeps the four branches from ever competing.
- SMA 20 runs over the same candles and publishes nothing until it is formed, so the first twenty candles of the run produce no signal at all. Two Comparison blocks read it: one is true while the close is below the average, the other while the close is above it.
- A Position block, a Variable holding zero and a Comparison set to Equal give the flat check. Both entry branches require it, so a week that is already in the market cannot stack a second position on top.
- Current time feeds Working time, which is true between 08:00:00 and 20:00:00. Both entry branches require that window as well, so a weekly position is never opened on a thin overnight candle. The two exits are deliberately left outside the window - whatever is open must be closed on its calendar day, at whatever hour the signal appears.
- Two Logical condition AND blocks collect the entries. The short branch needs the short day, a close below SMA 20, a flat position and an open window; the long branch needs the long day, a close above SMA 20, and the same two gates. Each drives a Modify position block with the Open position condition, so a repeated signal inside the same day cannot send a second order.
- The two exits are Modify position blocks with the Close position condition and an explicit side. The cover-day block is a buy, so it can only ever close a short; the exit-day block is a sell, so it can only ever close a long. Neither carries a volume input, because Close position sizes the order from the open position itself.
- The Chart panel draws the candle series, SMA 20, the orders of all four actions and the fills they produce, so the weekly rhythm - entry early in the week, cover midweek, entry late in the week, exit at the end - can be read straight off the picture.

## Entry and Exit Rules

- **Long entry**: On the long day, inside the entry window, with the position flat and the close above SMA 20, a market buy of Order volume is sent through Modify position with the Open position condition.
- **Short entry**: On the short day, inside the entry window, with the position flat and the close below SMA 20, a market sell of Order volume is sent through Modify position with the Open position condition.
- **Exit**: Exits are by calendar, not by price. On the cover day a Modify position buy with the Close position condition flattens an open short; on the exit day a Modify position sell with the same condition flattens an open long. There is no stop, no target and no trailing rule, so a position is carried until its own exit day arrives. The two exit signals repeat on every candle of their day, and nothing counts or suppresses that repetition: the first candle closes the position, and from then on Close position has nothing to work with and rejects the signal silently. The same holds for the entries - there is no per-day counter and no cooldown between trades, and it is the Open position condition together with the flat check that keeps a calendar day to a single order.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series. Only finished candles are delivered; the day of the week, the average and every signal are read from completed candles. |
| MA Period | 20 | Period of the simple moving average that filters both entries. The average publishes values only once it is formed, so no entry is possible during the first candles of a run. |
| Session From | 08:00:00 | Start of the daily window in which entries are allowed, read from the strategy clock. Exits ignore this window. |
| Session Until | 20:00:00 | End of that window. Widen the pair to let the calendar rule act at any hour, narrow it to concentrate entries in a few hours of the day. |
| Short day | 1 | Day number that opens a short when the close is below the average. Days are numbered from Sunday as 0 through Saturday as 6. |
| Cover day | 3 | Day number on which an open short is bought back. It closes a short only; on that day a long is left untouched. |
| Long day | 4 | Day number that opens a long when the close is above the average. Numbered on the same Sunday-as-0 scale. |
| Exit day | 5 | Day number on which an open long is sold out. It closes a long only; on that day a short is left untouched. |
| Order volume | 1 | Fixed quantity used by both Open position actions. The two Close position actions need no volume, because they size their order from whatever is open. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block is set to finished candles only, which matters twice: the day of the week is taken from a candle that will not change again, and every order the diagram sends is stamped with the closing time of a completed candle rather than the opening time of one still forming.
- Both calendar and price come from one [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) pair over that series. Reading the day of the week off the candle rather than off a separate clock keeps the calendar test on exactly the same beat as the trend test, so the two always describe the same candle when the [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND blocks collect them.
- The four day numbers are ordinary [Variables](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) compared with [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks, which is why the whole weekly plan can be rearranged from the parameter list: move the short day to another number and the schema trades that day instead, with no link touched.
- [Current time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) and [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) supply the hour-of-day gate as a plain level: true for the whole window, false outside it. It is wired into the two entry branches only, and the [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) snapshot next to it supplies the flat check the same way.
- Four [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks do all the trading: two with Open position and a fixed volume, two with Close position and a stated side. Giving the closing blocks a side is what makes the calendar exits exact - a cover-day buy simply refuses a long, and an exit-day sell simply refuses a short. Everything they emit is drawn on the [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) together with the candles and SMA 20.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
