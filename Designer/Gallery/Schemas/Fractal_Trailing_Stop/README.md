# Fractal Trailing Stop Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A fractal is a candle whose high is the highest of five, or whose low is the lowest of five, and it can only be named two candles after it happened. The diagram keeps that delay honest: it works on finished candles alone, turns every fractal into a stop price, and lets that price move in one direction only. Every break of a stop reverses the position, and the level that was broken steps aside until a new fractal arms it again.

![schema](schema.svg)

## Strategy Overview

- One candle series feeds everything, and it is set to deliver finished candles only, so no level and no order is ever built from a price a later tick could still take back.
- Highest and Lowest take the extreme of the last five candles, while Previous value hands back the candle two bars ago and two converters read its high and its low.
- When the high two bars back is the highest of the window, that candle is an upper fractal; the mirror test on the low marks a lower fractal. A latching variable stores each fractal price and lets it out only on the bar where the test passed.
- A formula adds the buffer percentage to the upper fractal price and takes it off the lower one, turning a fractal into a stop price.
- Two more formulas ratchet those prices with min and max: the upper stop may only fall and the lower stop may only rise, which is what makes them trailing stops instead of plain swing levels.
- The close of every finished candle is measured against both stops: above the upper stop the diagram reverses to long, below the lower stop it reverses to short.
- A reversal is a pair of blocks, one closing what is open and one opening the new side, and the stop that was just used is parked out of reach for as long as the position it opened is alive.
- The candles, both stop levels and every fill are drawn on one chart area, so the ratchet can be read straight off the picture.

## Entry and Exit Rules

- **Long entry**: The close of a finished candle rises above the upper trailing stop. Position modify in Close mode buys back the short if one is open, and Position modify in Open mode buys the order volume as soon as the account is flat.
- **Short entry**: The close of a finished candle falls below the lower trailing stop while the price is still under the upper one. The same pair works the other way round: the long is closed first, then the order volume is sold.
- **Exit**: There is no separate exit rule. A position is carried until the opposite stop is broken, and that break both closes it and opens the reverse side, so the diagram is always either long, short, or one fill away from it. The trailing stop tightens as new fractals appear, which is what moves the exit price along behind an open trade.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the single candle series. Only finished candles are delivered, so a decision is taken once per bar. |
| Upper Fractal Length | 5 | Number of candles the upper extreme is measured over. The candle under test sits in the middle of that window. |
| Lower Fractal Length | 5 | The same window for the lower extreme; keep it equal to the upper one, or the two sides will read fractals of different widths. |
| Fractal Shift | 2 | How far back the candle under test sits. It has to be the centre of the window: two for a window of five, three for a window of seven. |
| Stop Buffer, % | 0 | Percent added to the upper fractal price and taken off the lower one before the level is used. Zero puts the stop exactly on the fractal; a larger value keeps it a little further away from the price. |
| Order Volume | 1 | Order size, in lots. The same size is used for both sides, so a reversal is a close of the old size followed by an open of this one. |

## Diagram Details

- Finished candles are what keeps the levels from repainting. The indicator block treats every value handed to it as final, so a candle still forming would be written into Highest and Lowest and then overwritten on its next update; delivering finished candles only means the two indicators never see a value that can change.
- The fractal test reads 'at or above' rather than 'above', so a flat top where two candles share the same high still counts as a fractal, and so does a double bottom on the low side.
- A fractal is named two candles late by construction, and the diagram does not try to hide that: the level it produces is the one that was true two bars ago, and it is used from the bar it becomes known on.
- While a position is open, the stop that opened it is parked far out of reach and re-arms from the first fractal that forms once the position is gone. Without that the diagram would keep signalling the side it is already on, and the ratchet would drag the level so far from the price that it could never be crossed again.
- Should both stops ever read as broken in the same instant, the long side takes precedence: the short leg carries the extra condition that the price is still under the upper stop, so the two can never fire together.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
