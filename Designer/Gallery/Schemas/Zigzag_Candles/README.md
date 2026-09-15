# Zigzag Candles Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram keeps two swing levels on the wire — the highest high and the lowest low of the last five finished hourly candles — and acts on the candle that reaches one of them. A pair of Flag blocks that reset each other allow a single action per swing, so a level touched again and again inside the same move produces one order and no more. Each swing first flattens whatever is open; the swing after it opens the opposite side.

![schema](schema.svg)

## Strategy Overview

- One hourly candle series feeds everything, and it delivers finished candles only, so no level and no order is ever built from a price a later tick could still take back.
- Two converters read the high and the low of each candle and feed a Highest of five and a Lowest of five. Both indicators publish only once they are formed, so no level exists until five candles have closed.
- Two Variable blocks hold the levels. Each takes the newest indicator value without releasing it and republishes what it holds on every candle, so a level stays available to the comparisons on every bar rather than only on the bar that changed it.
- Two more converters read the high and the low of the candle that has just closed, and two Comparison blocks measure them against the held levels: a high that reaches or exceeds the upper level, a low that reaches or undercuts the lower one.
- A Position snapshot is latched once per candle and compared with zero twice, giving a not-short test and a not-long test. Each of the two breaks is joined with the matching test by a Logical condition AND, so the upper level is acted on only while the position is not already short, and the lower level only while it is not already long.
- Every AND drives the Trigger of a Flag, and the opposite break drives that Flag's Reset. A Flag passes one true and then falls silent until it is reset, which turns a level touched many times inside one swing into exactly one action.
- A Flag that fires reaches a Modify position set to Close position and a Modify position set to Open position at the same time. Only one of them applies: an existing position is flattened at market, a flat one is opened on the side the swing calls for, and the block that does not apply rejects the signal.
- The chart panel draws the hourly candles, both extreme lines, all three order streams and every fill, so the swing levels and the actions taken on them can be read off one picture.

## Entry and Exit Rules

- **Long entry**: The low of a finished candle reaches or drops below the held lower level while the position is not long. The lower Flag fires once: a short position is bought back at market and becomes flat, and a flat position is turned into a long of Order Volume. The Flag then stays silent, however often the low is revisited, until the upper level is taken.
- **Short entry**: The high of a finished candle reaches or exceeds the held upper level while the position is not short. The upper Flag fires once: a long position is sold at market and becomes flat, and a flat position is turned into a short of Order Volume. That Flag stays silent until the lower level is taken.
- **Exit**: There is no stop, no target and no timer - the position is closed by the opposite swing. The same Flag that opens one side also feeds the Close position block, so the first touch of the other level flattens whatever is open. Because the entry volume is fixed and the opening block only accepts a flat position, a full reversal always takes two swings: one to flatten, the next to open the other side.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 01:00:00 | Time frame of the candle series. Only finished candles are delivered, so a decision is taken once per bar. |
| Swing High Length | 5 | Number of finished candles the upper extreme is taken over. A longer window makes the upper level harder to reach and the swings longer, a shorter one turns almost every candle into a new extreme. |
| Swing Low Length | 5 | Number of finished candles the lower extreme is taken over. It is kept separate from the upper one so the two sides can be made deliberately asymmetric. |
| Initial Swing High | 999999999 | Value the upper level holds until the upper indicator is formed. It is deliberately unreachable, so that no high can touch it during the warm-up and no short can be opened before a real extreme exists. |
| Initial Swing Low | 0 | Value the lower level holds until the lower indicator is formed. Zero cannot be reached from above by a traded price, which keeps the long side quiet for the same reason. |
| Order Volume | 1 | Quantity used by both opening actions. The closing action needs no volume - it flattens whatever is open - so with a fixed quantity the position only ever moves between one lot short, flat and one lot long. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block is subscribed to finished candles only, and that is what makes the levels honest. The indicators are fed through [Converters](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html), and a converter emits a plain number, which always counts as final; a candle still in progress would therefore enter the Highest and Lowest windows as if it were complete and the extreme would end up containing itself. The same subscription is what keeps the orders legal: an order built from an update of an unfinished candle carries that bar's opening time and is refused as arriving from the past.
- A break is measured against the levels as they stood before the candle being judged: the holding variables republish what they already had, and the indicators take the new candle in on the same bar, so the level a candle is tested against is the extreme of the five candles that came before it rather than one that includes itself.
- The two [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) blocks are wired crosswise: the upper break resets the lower Flag and the lower break resets the upper one. That pair is the whole memory of the diagram - it says which way the last swing went and whether it has already been acted on. A Flag ignores a false on either socket, so the comparisons publishing false on every candle update cost nothing, and it emits again only after the opposite level has been reached.
- The levels are held by [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) blocks whose input does not act as a trigger; the candle series drives their Trigger instead. The same pattern latches the [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) value once per candle, so the two position [Comparisons](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) and the two [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND blocks all read the same snapshot rather than a value that shifts underneath them.
- Three [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks act, all at market: one with Close position that takes both Flags, and one with Open position for each side. On an outside candle, one that both takes out the previous high and undercuts the previous low, both Flags can fire in the same pass and two market orders are sent; they cancel each other out in the position but both appear as fills in the log and on the [Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html).

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
