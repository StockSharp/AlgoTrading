# Day of Week Effect Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The calendar decides the direction and the moving average decides the timing. Early in the week the diagram is allowed to buy, late in the week it is allowed to sell, and in both cases it waits for the closing price to be on the matching side of a simple moving average before it acts. The day of the week is read straight off the candle, so no state has to be carried from one candle to the next.

![schema](schema.svg)

## Strategy Overview

- A converter reads the weekday out of the candle's opening time as a number, where Sunday is zero and Saturday is six.
- Two comparisons form each calendar window: Monday to Tuesday for the long side, Thursday to Friday for the short side, with the boundaries exposed as parameters so the window can be moved or widened.
- A simple moving average of the closing price confirms the direction; the calendar alone never opens a trade.
- The current position takes part in both entries, so the diagram never adds to a trade it already holds.

## Entry and Exit Rules

- **Long entry**: The candle belongs to the early week window, its close is above the simple moving average and the position is flat. The order buys the shared volume at market.
- **Short entry**: The candle belongs to the late week window, its close is below the simple moving average and the position is flat. The order sells the shared volume at market.
- **Exit**: A close back below the average closes a long and a close back above it closes a short, both through position modify blocks in close mode. Because a close block does nothing while the position is already flat, this reproduces the crossing test of the original without any extra blocks. The original knows two counters the diagram cannot keep between candles, and both were dropped: the pause of three hundred bars after every trade and the rule that forbids a second entry on the same weekday. Without them the diagram re-enters as soon as the price returns to the right side of the average inside the same window, so it trades noticeably more often than the original.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| MA Period | 20 | Length of the simple moving average that confirms the direction and closes the trades. |
| Long day from | 1 | First weekday of the long window, as a number, with Sunday zero. One is Monday. |
| Long day to | 2 | Last weekday of the long window. Two is Tuesday. |
| Short day from | 4 | First weekday of the short window. Four is Thursday. |
| Short day to | 5 | Last weekday of the short window. Five is Friday. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the moving average and two converters, one for the closing price and one for the weekday of the opening time.
- Four comparisons place the weekday inside or outside the two calendar windows, and two more place the close on one side or the other of the average.
- Each logical AND joins both ends of a calendar window, the side of the average and the flat check before triggering an entry block.
- The two closing blocks hang directly on the average comparisons and carry the close-position condition, so each of them only ever flattens its own side.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
