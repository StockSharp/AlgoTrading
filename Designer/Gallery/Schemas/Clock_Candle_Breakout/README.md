# Clock Candle Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram picks one reference candle each day by the clock, remembers its high and low, and trades a break of those two levels during the next three finished candles. An EMA 20 filter decides which side of the break is tradable, the window closes the position when it expires, and a ten-candle cooldown keeps the diagram out of the market for a while after every accepted entry.

![schema](schema.svg)

## Strategy Overview

- Thirty-minute candles are delivered both while they are still forming and once they are finished. A Final value block splits that stream: the reference level, the window counter and the cooldown counter see finished candles only, while the breakout test reads the close of the candle currently forming.
- Working time marks the reference candle: the finished candle whose opening time falls between 02:30:00 and 02:59:59. On a thirty-minute frame exactly one candle a day qualifies, and the half-hour span leaves room for the frame to be changed without losing the daily pulse.
- Two pairs of Variable blocks lift the level off that candle. In each pair the first variable stores the high (or the low) of every finished candle and releases it only when the Working time pulse arrives; the second holds what was released and repeats it on every candle update, so the level stays on the wire between reference candles.
- The same pulse arms an N values counter set to three. It counts finished candles and fires once the third one after the reference candle closes, which is what ends the trading window.
- Window state is one numeric Variable written through a Combination from three sources: one when the reference candle is taken, zero when the three-candle counter fires, and zero as soon as an entry is accepted. A Comparison against zero turns that number into the gate both entry branches read, so a window yields at most one position.
- A long needs the close above the reference high and above EMA 20; a short needs the close below the reference low and below EMA 20. Each side is a Logical condition AND that also requires an open window, an elapsed cooldown and a flat position.
- An accepted entry sends a market order through Modify position with the Open position condition, so a signal that repeats inside the same candle cannot stack a second order on top of the first. The same signal arms a second N values counter of ten finished candles; a second Variable pair, joined by its own Combination, holds the cooldown flag at zero until that count runs out and restores it to one afterwards.
- When the three-candle counter fires, two Modify position blocks with the Close position condition receive it. The one whose side opposes the open position flattens it at market; the other has nothing to close and rejects the signal.

## Entry and Exit Rules

- **Long entry**: During the three finished candles that follow the reference candle, with the cooldown elapsed and the position flat, a close above the reference high and above EMA 20 sends a market buy of Order Volume through Open position.
- **Short entry**: During the same three candles, with the cooldown elapsed and the position flat, a close below the reference low and below EMA 20 sends a market sell of Order Volume through Open position.
- **Exit**: The position is closed by time, not by price: when the three-candle window counter fires, the Close position blocks flatten whatever is open at market. There is no stop, no target and no trailing rule, so the holding time never exceeds the window, and the accepted entry also writes zero into the window so the same window cannot be traded twice.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:30:00 | Time frame of the candle series. Forming and finished candles are both delivered; only finished ones define the reference level and drive the two counters. |
| EMA Length | 20 | Period of the exponential moving average that decides which side of the break may be traded. Values are published only once the average is formed, so no entry is possible before then. |
| Reference From | 02:30:00 | Start of the daily span in which the reference candle is looked for, read from the opening time of each finished candle. |
| Reference Until | 02:59:59 | End of that span. Together with the start it must cover exactly one candle opening per day; the default pair spans one thirty-minute candle. |
| Window Bars | 3 | Number of finished candles the trading window lasts after the reference candle. The same counter closes the position when it expires. |
| Cooldown Bars | 10 | Number of finished candles counted after an accepted entry before the diagram is allowed to trade again. |
| Order Volume | 1 | Fixed quantity used by both Open position actions. Close position actions need no volume, because they flatten whatever is open. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block publishes forming and finished candles alike, and [Final value](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/final_value.html) is what separates them. Three [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) read High and Low behind Final value and Close in front of it, which is why the level always comes from a completed candle while the break is tested against a live price.
- [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) reads the opening time of the finished candle sent into it, so its output is true for one candle a day rather than for a span of wall-clock time. Order matters on the diagram: the High and Low converters are linked ahead of it, so the storing [Variables](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) already hold the current candle when the pulse releases them.
- Each of the two [N values](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) counters is armed by a signal and counts finished candles: three for the trading window, ten for the cooldown. Their outputs meet the opening and locking values in two [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) blocks, and each Combination feeds one state Variable that republishes its number on every candle update.
- The [Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) block supplies EMA 20. Seven [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) blocks build the two breakout tests, the two trend tests, the window gate, the cooldown gate and the flat-position check against a [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) snapshot held per candle; two [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND blocks collect them into the two entry branches, and an OR block turns either branch into the single signal that starts the cooldown.
- Four [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks act: two with Open position for the entries and two with Close position for the timed exit. Every fill is gathered by a Combination and drawn, with the candles, EMA 20, both reference levels and all four order streams, on the [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html).

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
