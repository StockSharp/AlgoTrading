# Tape Reader Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A candle says what happened during a bar; the tape says how it happened. This diagram subscribes to the stream of executed trades, measures every print against the average size of the last hundred prints, and treats a print several times that average as the footprint of someone in a hurry. The reading is taken once per finished candle, so a stream that fires thousands of times a day still produces one decision per bar.

![schema](schema.svg)

## Strategy Overview

- The tick stream carries both halves of the signal: one converter reads the size of every executed print, another reads its price.
- A moving average over the last hundred print sizes gives the diagram a running idea of what an ordinary trade looks like on this instrument, so nothing in it is tied to a particular price level or contract size.
- A formula divides the size of each print by that average and turns it into a plain multiple: one is an ordinary print, five is a print five times the recent norm.
- A variable holds that multiple, and a second variable holds the print price, until the candle closes. The candle is their trigger, which is what puts a tick-speed measurement and a candle-speed decision on the same clock.
- A comparison against the size factor answers whether the print was a large one. The previous candle's close, taken with a Previous value block and a converter, answers which way it went.
- The Position block is compared with zero three times, giving a flat test for entries and a long and a short test for exits.
- Strategy trades reports every own fill and resets a candle counter, which keeps the diagram off the market for a set number of candles after any fill, entry or exit.
- Both entries are Position modify blocks set to open only, and the exit is a third one set to close, so a single position is held at a time and it is flattened before the opposite side can be taken.

## Entry and Exit Rules

- **Long entry**: The print that went through last before the candle closed was at least Size factor times the average print size, its price is above the previous candle's close, the position is flat, and the cooldown has elapsed. Position modify buys the order volume at market.
- **Short entry**: The same large print, but priced below the previous candle's close, again from a flat position and with the cooldown elapsed. Position modify sells the order volume at market.
- **Exit**: There is no take-profit and no stop-loss. A long is closed when a large print comes through below the previous close while the position is long, and a short is closed by a large print above it. Both exit gates feed one Combination, which triggers the Position modify block set to close; the fill that flattens the position also restarts the cooldown, so the next entry waits out the same number of candles.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candles the whole diagram works on; every reading of the tape is taken when one of them closes. |
| Average Prints | 100 | How many of the most recent prints form the average size the multiple is measured against. A shorter window follows a change of activity faster and makes the average itself jumpier. |
| Size Factor | 5 | How many times the average size a print has to reach to count as a large one. Raise it for rarer and more selective signals; lower it when the tape is quiet and nothing qualifies. |
| Cooldown Bars | 3 | How many finished candles must pass after any fill before the next entry is allowed. |
| Order Volume | 1 | Size of each entry order, in instrument units. The closing order is sized from the open position and does not read this value. |

## Diagram Details

- The measure is a ratio, not a number of contracts, so the same size factor reads the same way on an instrument quoted in fractions of a unit and on one quoted in whole lots.
- The average includes the print being measured against it, so a single very large print lifts its own yardstick a little; with a hundred prints in the window a print five times the norm still reads above four.
- The print that is read is the last one to arrive before the candle closed. The variable holding it is what stands between a stream that fires thousands of times a day and a decision meant to be taken once a bar; without it the size test and the price test would answer on different clocks and never agree.
- The cooldown counter is reset from the strategy-fills block rather than from the entry blocks, so a closing fill starts the wait as well. Without it the exit of one trade and the entry of the next would land on neighbouring candles.
- The chart panel draws the candles, the average print size, the held multiple and the size factor line, the entry and exit orders and every own fill, so the print that produced a trade can be found beside the candle it belongs to.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
