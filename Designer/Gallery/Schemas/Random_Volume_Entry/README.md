# Random Size Alternating Entry Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Every diagram in the gallery decides how much to trade; this one refuses to. The Random block draws a fresh size between half a lot and two on every candle and feeds it straight into the volume socket of both entry blocks, so no two positions are the same size. The direction is not random at all: it comes from where the last executed print stands against the open of the running candle.

![schema](schema.svg)

## Strategy Overview

- The tick stream is the signal. A converter reads the price of the last executed trade, and a variable holds it until the candle closes, which is what puts the print and the candle open on the same clock.
- A comparison asks whether that print is above the candle open. The same answer inverted by a logical NOT is the short side, so one comparison serves both directions.
- The Random block is triggered by the candle and hands its number to the volume socket of the buy and the sell block. Nothing else in the diagram reads it.
- Entries are taken from a flat position only, and both blocks carry the open-position condition, so a signal cannot add to a position that is already open.
- A counter of candles since the last fill keeps twelve candles between entries. Without it the protective exit and the next entry chase each other on consecutive candles.
- Position protection is the only exit. It takes the entry fills through a Combination, prices them off the candle close, and closes at a 0.4% target or a 0.5% trailing stop.
- The stop trails, so a position that runs in the right direction gives back only the last part of the move.
- The chart panel draws the candles, both order streams and every fill, including the protective ones.

## Entry and Exit Rules

- **Long entry**: The last executed print is above the open of the running candle, the position is flat and twelve candles have passed since the last fill. Position modify buys at market, with the volume the Random block drew for this candle.
- **Short entry**: The last executed print is at or below the open of the running candle, under the same flat-position and cooldown conditions. Position modify sells at market with the same randomly drawn volume.
- **Exit**: There is no exit signal. Position protection owns the position from the first fill and closes it at a 0.4% take-profit or a 0.5% trailing stop measured from the entry price. Any fill, protective ones included, restarts the twelve-candle wait before the next entry.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candles the whole diagram works on. |
| Cooldown Bars | 12 | How many finished candles must pass after a fill before the next entry is allowed. |
| Min Volume | 0.5 | Smallest size the Random block may draw. |
| Max Volume | 2 | Largest size the Random block may draw. |
| Take Profit, % | 0.4 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 0.5 | Trailing stop distance, in percent of the entry price. |

## Diagram Details

- The candle block feeds eight consumers: both converters, the print variable, the Random block, the cooldown counter and its two variables, and the chart panel.
- The print variable is the only thing standing between a tick stream that fires thousands of times a day and a condition meant to be answered once per candle.
- Both entry blocks share one Random output, so long and short are sized by the same draw; the number changes on the next candle, not between the two blocks.
- The cooldown counter is reset from the strategy-fills block, which is why a protective exit also starts the wait, not only an entry.
- The size is drawn with two decimals, which suits an instrument quoted in fractions of a unit; on a whole-lot instrument the range is set in whole numbers instead.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
