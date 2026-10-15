# ADX / DI Crossover Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Welles Wilder's directional movement system in one diagram. The Average Directional Index block delivers three numbers at once: the +DI line, the -DI line and the ADX line itself. The crossing of the two directional lines decides the side of the trade, while the ADX line decides whether the market is trending enough to take it at all.

![schema](schema.svg)

## Strategy Overview

- One AverageDirectionalIndex block feeds three converter blocks that read +DI, -DI and the ADX line out of the same complex indicator value.
- A crossing block watches +DI against -DI and fires only on the bar where the two lines actually swap places.
- The ADX line has to stand at or above the threshold, so flat and directionless stretches are filtered out.
- A formula block adds the absolute position to the base volume, so a single market order both closes the old side and opens the new one.

## Entry and Exit Rules

- **Long entry**: +DI crosses above -DI, the ADX line is at or above the threshold and the position is not already long. The order buys the base volume plus the size of any short, which reverses a short or opens a long from flat.
- **Short entry**: +DI crosses below -DI, the ADX line is at or above the threshold and the position is not already short. The order sells the base volume plus the size of any long, which reverses a long or opens a short from flat.
- **Exit**: There is no separate exit block. A position lives until the directional lines cross the other way, and the reversing order both closes it and opens the opposite one.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| ADX Period | 14 | Averaging length shared by the ADX line and by the +DI/-DI pair. |
| ADX Threshold | 15 | Minimum ADX reading that counts as a tradable trend. |
| Volume | 1 | Base order volume, in lots; the size of the open position is added on top of it. |
| Candles | 00:15:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the indicator block; three converter blocks pull Dx.Plus, Dx.Minus and MovingAverage out of its value.
- The crossing block emits true when +DI goes above -DI and false when it goes below, so a logical NOT turns the same output into the short signal.
- One comparison tests the ADX line against the threshold constant; two more compare the position against zero, one per side.
- Each logical AND joins the crossing, the trend filter and the position guard, and triggers a position modify block whose volume comes from the formula block.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
