# Moving Average Crossing with Volume Confirmation Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A moving average crossing on its own reacts to every twitch of the price. This diagram accepts a crossing only when it arrives together with a real jump in activity: the candle that crosses the SimpleMovingAverage has to trade more than the candle before it by a set factor. The opposite crossing gives the position back, and there no volume is asked for.

![schema](schema.svg)

## Strategy Overview

- A SimpleMovingAverage of the candle is the line the close has to cross, and a single crossing block turns the two series into one up-or-down event.
- The volume filter compares the candle against its own predecessor, not against an average: a previous-value block keeps the volume of the candle before, a formula multiplies it by the factor and a comparison checks the new candle against the result.
- Entries are taken only from a flat position and only with the volume confirmation; exits are taken on the reverse crossing alone, exactly as the C# original does it.
- The original freezes trading for 150 bars after every order; a bar counter has no block of its own, so that pause is left out and this diagram trades more often.

## Entry and Exit Rules

- **Long entry**: The close crosses the moving average upwards, the volume of that candle is above the previous candle's volume multiplied by the factor, the previous volume itself is above zero, and the position is flat. The modify block buys the shared volume at market.
- **Short entry**: The close crosses the moving average downwards under the same volume confirmation and with a flat position. The modify block sells the shared volume at market.
- **Exit**: A long is closed by the first downward crossing and a short by the first upward crossing, with no volume condition attached; both closing blocks run in close-position mode, so they act only when there is something to close. Neither the source strategy nor this diagram carries a stop loss or a take profit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the Simple Moving Average the close has to cross. |
| Volume factor | 1.2 | How many times the previous candle's volume the current candle must exceed for an entry to be accepted. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds a converter for the total volume, a converter for the close price and the moving average.
- The volume chain is previous value, formula and comparison; a second comparison against zero keeps the very first candle from passing the filter for free.
- One crossing block plus a logical NOT covers both directions: the block's own output is the upward crossing, the negated one is the downward crossing.
- Two logical ANDs assemble the entries out of crossing, volume and a flat position, and two more assemble the exits out of the opposite crossing and the sign of the position.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
