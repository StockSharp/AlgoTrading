# TSI Signal Line Crossover Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The True Strength Index is momentum smoothed twice, so it turns late but rarely lies. Read against its own exponential signal line it behaves like a slow MACD: the crossing names the direction and the distance between the lines says how convincing the turn is. This diagram only takes crossings where that distance already exceeds a minimum, which is what separates a real change of control from the lines brushing against each other.

![schema](schema.svg)

## Strategy Overview

- One True Strength Index block carries both lines; two converter blocks pull the TSI line and its signal line out of the same value.
- A crossing block compares the two lines and reports the direction of the cross; a logical NOT turns the same output into the downward cross.
- A formula measures the absolute gap between the lines, and a comparison demands that it is at least the minimum spread before the cross is accepted.
- The position guard decides whether an entry is allowed, and the order volume is the shared volume plus the absolute position, so an opposite signal reverses in one order.

## Entry and Exit Rules

- **Long entry**: The TSI line crosses above its signal line, the gap between them is at least the minimum spread and the position is not long. The order buys the shared volume plus the size of an open short, so one market order closes the short and opens the long.
- **Short entry**: The TSI line crosses below its signal line, the gap between them is at least the minimum spread and the position is not short. The order sells the shared volume plus the size of an open long.
- **Exit**: There is no exit rule of its own and no protective stop, exactly as in the original: a position is held until the opposite crossing reverses it. Two things are simplified. The original waits ten candles after every entry before it looks at signals again, and no block keeps a bar counter between candles, so that pause is dropped; the position guard still prevents a second entry in the same direction. The original also fires two market orders when it reverses, which doubles the size for an instant; here the volume formula does the same job in a single order.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| TSI First Length | 25 | First smoothing period of the True Strength Index. |
| TSI Second Length | 13 | Second smoothing period of the True Strength Index. |
| TSI Signal Length | 7 | Length of the exponential signal line drawn on the index. |
| Min spread | 2 | Minimum absolute gap between the index and its signal line for a crossing to count. |
| Volume | 1 | Order volume, in lots. |
| Candles | 01:00:00 | Candle time frame the whole diagram works on. The original runs on four hour candles; on one month of history that leaves too few finished bars for a double smoothed index to form and still trade, so the diagram is scaled down to hourly candles. |

## Diagram Details

- The candle block feeds the True Strength Index block, whose complex value is split by two converter blocks into the index and its signal line.
- The crossing block takes the index as the upper input and the signal line as the lower one, so its output is true on an upward cross and false on a downward one.
- The gap formula and its comparison run on every candle, while the crossing block speaks only on crossings, so each logical AND fires exactly on the bar where a filtered crossing happens.
- Both position modify blocks take their volume from one formula that adds the absolute position to the shared volume constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
