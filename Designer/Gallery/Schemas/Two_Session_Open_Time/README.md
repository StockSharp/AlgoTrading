# Two Session Open Time Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram carries no indicator at all: the clock is the only source of signals. Two separate windows of the trading day each open one long position, a Flag keeps every window to a single entry per day, and a third window flattens whatever is still open and re-arms both windows for the day that follows.

![schema](schema.svg)

## Strategy Overview

- Time streams the current moment into three Working time blocks: two entry windows, 09:30-14:00 and 00:00-04:00, and one forced-close window, 19:50-20:00.
- Each entry window is combined with a flat-position check by a Logical condition set to And, so a window can ask for an entry only while nothing is open.
- A window stays open for hours and its gate keeps repeating the same true value. Flag stands between the gate and the order and lets through only the first of them, which turns a long window into one entry.
- Both windows buy. Position modify runs with the Open position condition, so a market order for Order Volume leaves only when the position is exactly zero.
- The forced-close window drives a third Position modify set to Close position, and the very same signal resets both Flags, so the two entry windows are armed again for the next day.
- Position protection watches the fills of both entries and closes the position at a 1.5% take-profit or a 0.5% trailing stop that follows the candle close.
- Finished five-minute candles pace the whole diagram: they carry the close price to Position protection, they are what the panel draws, and their arrival is what moves the clock forward.
- The chart panel shows the candles, the price line the protection reacts to, every order the diagram sends and every fill it receives.

## Entry and Exit Rules

- **Long entry**: Inside either window, while the position is flat, the window's Flag releases its first true signal and Position modify buys Order Volume at market under the Open position condition. Every later signal of the same window is swallowed by the Flag until the close window resets it.
- **Short entry**: There is no short side. Both windows open long, and the only sell orders the diagram ever sends are the ones that close an open long.
- **Exit**: Position protection closes the position at a 1.5% take-profit or a 0.5% trailing stop that follows the candle close. Anything still open when the close window begins is flattened by the Close position action, which also clears both latches.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Five-minute time frame; only finished candles are processed, and their closes are what the protection price checks and the chart line are built from. |
| First Window From | 09:30:00 | Start of the first entry window in replay or server time. |
| First Window Until | 14:00:00 | End of the first entry window; after it that window can no longer arm an entry. |
| Second Window From | 00:00:00 | Start of the second entry window in replay or server time. |
| Second Window Until | 04:00:00 | End of the second entry window. |
| Close Window From | 19:50:00 | Start of the forced-close window, which flattens an open position and resets both latches. |
| Close Window Until | 20:00:00 | End of the forced-close window. |
| Order Volume | 1 | Fixed quantity used by both window entries. |
| Take Profit, % | 1.5 | Favourable percentage move at which Position protection closes the position. |
| Stop Loss, % | 0.5 | Adverse percentage move of the stop; the trailing technique moves it up behind the candle close once price runs in favour. |

## Diagram Details

- The [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) block emits finished five-minute candles. A [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) takes their close price, which is the price [Position protection](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) measures its take-profit and trailing stop against and the line drawn beside the candles. Nothing else is calculated from price: the diagram holds no indicator.
- [Time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) supplies the current moment to three [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) blocks. Two of them mark the entry windows and one marks the forced-close window; the packaged-history replay runs in UTC, so the window boundaries are read as UTC times.
- [Position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) compared with a zero [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) through [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) produces the flat check that both [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) And gates share, so an open position silently blocks the other window as well.
- [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) is what makes a window a single event. Its trigger is the And gate and its reset is the close window; it passes a value only at the moment it is set, so the hundreds of true readings a four-hour window produces collapse into one entry.
- Three [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) blocks act: two Open position entries that take Order Volume, and one Close position flatten that needs no volume because it reads the position it has to undo. Both entry fills are joined by [Combination](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) and handed to Position protection, whose own closing fill is drawn on the panel but is not fed back into its trade input.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
