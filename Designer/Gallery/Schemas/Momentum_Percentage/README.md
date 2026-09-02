# Momentum Zero Cross with SMA Filter Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two ideas are stacked here. Momentum, the difference between the current close and the close ten candles ago, says whether the last stretch of the market pushed price up or down, and the sign change of that difference is the trigger. A simple moving average then acts as the referee: the cross is only taken in the direction the close already agrees with.

![schema](schema.svg)

## Strategy Overview

- Momentum crossing the zero line is spelled out as two comparisons, the current value against zero and the value one candle back against zero, which is exactly the condition the original code writes.
- The moving average filter keeps the cross up and the cross down apart: a cross up is only a buy while the close is above the average, a cross down only a sell while it is below.
- Despite the folder name the indicator is Momentum, an absolute price difference in points, not a percentage rate of change.
- Every signal reverses the position: the order volume is the shared volume plus the absolute value of the current position, so a single fill closes the old side and opens the new one.
- The original freezes trading for 30 candles after each fill; there is no bar-counting block, so that pause is left out and the diagram reacts to every qualifying cross.

## Entry and Exit Rules

- **Long entry**: Momentum was at or below zero on the previous candle, is above zero now, the close is above the SMA and the position is not long. The order buys the reversal volume at market.
- **Short entry**: Momentum was at or above zero on the previous candle, is below zero now, the close is below the SMA and the position is not short. The order sells the reversal volume at market.
- **Exit**: There is no separate exit block and no protective stop, exactly as in the original: a position is held until the opposite cross reverses it in one order.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Momentum Length | 10 | Number of candles Momentum looks back over; the value is the current close minus the close that many candles ago. |
| SMA Length | 20 | Averaging length of the simple moving average that filters the direction of the cross. |
| Volume | 1 | Base order volume, in lots; the reversal order adds the absolute value of the open position to it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds three branches: the Momentum indicator, the simple moving average and a converter that takes the close price.
- A previous-value block holds the Momentum reading of the last candle, and four comparison blocks put the current and the previous reading on either side of a shared zero constant.
- Two more comparison blocks put the close against the moving average, and two compare the position against the same zero constant.
- Each logical AND joins the previous side of zero, the current side of zero, the moving average filter and the position guard, then triggers a position modify block whose volume comes from a formula computing volume plus the absolute position.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
