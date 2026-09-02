# Williams %R Level Cross Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Williams %R says where the close sits inside the range of the last candles, from 0 at the top to -100 at the bottom. This diagram trades the moment the oscillator walks into a zone rather than the moment it leaves one: a drop through the low level buys, a rise through the high level sells. Percent protection takes the trade off.

![schema](schema.svg)

## Strategy Overview

- Williams %R with a length of 14 is calculated on finished hourly candles, which the tester builds from the packaged five-minute history.
- The signal is the crossing itself: the previous reading on one side of the level and the current reading on the other, so a long stay inside a zone fires only once.
- This is the entry into the zone, the mirror image of the classic reading that waits for the oscillator to climb back out, and it matches the Direct mode of the original strategy.
- The original also carries switches that allow long and short entries separately; both are on by default, so the diagram wires both sides and a branch can simply be unplugged to disable one.

## Entry and Exit Rules

- **Long entry**: The previous %R was above the low level and the current one is at or below it, and the position is not long. The order buys one lot, which opens a long from flat or closes an existing short.
- **Short entry**: The previous %R was below the high level and the current one is at or above it, and the position is not short. The order sells one lot, which opens a short from flat or closes an existing long.
- **Exit**: The protection block closes the trade at a take profit of 2 percent or a stop loss of 1 percent from the entry price; before that, the opposite crossing flattens the position, because every order uses the same volume.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Williams %R Length | 14 | Look-back length of Williams %R. |
| Low Level | -80 | Level the oscillator has to drop through to arm a long entry. |
| High Level | -20 | Level the oscillator has to rise through to arm a short entry. |
| Take profit, % | 2 | Take profit distance from the entry price, in percent. |
| Stop loss, % | 1 | Stop loss distance from the entry price, in percent. |
| Volume | 1 | Order volume, in lots. |
| Candles | 01:00:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the indicator block with Williams %R, and a previous-value block keeps the reading from one candle back.
- Four comparison blocks build the two crossings out of the previous and the current reading against the two level constants.
- Two more comparison blocks test the position against a zero constant, and each logical AND joins one crossing with its position guard.
- Both modify blocks send market orders with the volume of one shared constant, and their own trades feed the protection block that carries the take profit and the stop loss.
- The original protects the position with absolute price distances; the diagram uses percentages of the entry price instead, so the same numbers work on any instrument.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
