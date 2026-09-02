# Momentum Zero-Line Impulse Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The whole diagram rests on one number: the difference between the current close and the close twelve candles ago. While that difference is positive the market has carried price up over the window, while it is negative it has carried it down, and the moment it changes sign the diagram flips its position. Despite the folder name the original uses Momentum, an absolute price difference, and not a percentage rate of change.

![schema](schema.svg)

## Strategy Overview

- Momentum over 12 candles is compared against the zero line, and the previous value of the same indicator says which side it came from, which turns two comparisons into a crossing.
- Every signal is a reversal: the order volume is the shared volume plus the absolute value of the current position, so one order closes the old side and opens the new one in a single fill.
- The position takes part in both branches, so a cross up is only bought while the book is not already long and a cross down is only sold while it is not already short.
- The original also freezes trading for 55 candles after each fill; a bar counter has no block of its own, so that pause is left out and the diagram reacts to every cross.

## Entry and Exit Rules

- **Long entry**: Momentum was at or below zero on the previous candle, is above zero now and the position is not long. The order buys the reversal volume at market, which closes any short and opens the long in one step.
- **Short entry**: Momentum was at or above zero on the previous candle, is below zero now and the position is not short. The order sells the reversal volume at market, which closes any long and opens the short in one step.
- **Exit**: There is no separate exit block. A position is held until the opposite zero-line cross reverses it, and the original has neither a stop loss nor the ATR stop its README mentions.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Momentum Length | 12 | Number of candles Momentum looks back over: the value is the current close minus the close that many candles ago. |
| Volume | 1 | Base order volume, in lots; the reversal order adds the absolute value of the open position to it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the Momentum indicator, whose output goes both to the comparison blocks and to a previous-value block that holds the reading of the last candle.
- Four comparison blocks share one zero constant, which also serves as the reference for the two position checks.
- Each logical AND joins the current side of zero, the previous side of zero and the position condition, then triggers a position modify block.
- A formula block computes the reversal size as the shared volume plus the absolute position and feeds the volume of both orders.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
