# Aroon Oscillator Sign Alert Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The Aroon Oscillator asks which is fresher, the highest high or the lowest low of the last few candles, and answers with a number between -100 and +100. This diagram does not trade the extreme itself, it trades the moment the market leaves it: a reading that climbs back above the down level buys, a reading that falls back below the up level sells. The original strategy runs on four-hour candles; the diagram works on five-minute candles so the packaged one-month history gives it enough bars to trade.

![schema](schema.svg)

## Strategy Overview

- AroonOscillator is calculated on finished candles of a single instrument and swings between -100 and +100.
- A previous-value block keeps the reading of the candle before, so a real level crossing is told apart from a bar that merely stands above the level.
- The two sides are deliberately asymmetric: the long is taken when a strong downward bias fades, the short when a strong upward bias fades.
- The current position takes part in both decisions, so an order never adds to a position that is already open.

## Entry and Exit Rules

- **Long entry**: The previous AroonOscillator reading was at or below the down level, the current one is above it, and the position is not long. The order buys one lot, which opens a long from flat or closes an existing short.
- **Short entry**: The previous AroonOscillator reading was at or above the up level, the current one is below it, and the position is not short. The order sells one lot, which opens a short from flat or closes an existing long.
- **Exit**: There is no exit block and no protective stop, just as in the original strategy: the opposite signal flattens the position, because every order carries the same volume.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Aroon Length | 9 | Number of candles the Aroon Oscillator looks back over. |
| Down Level | -50 | Lower level; the oscillator crossing it upwards is the buy signal. |
| Up Level | 50 | Upper level; the oscillator crossing it downwards is the sell signal. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on; the original strategy used four hours. |

## Diagram Details

- The candle block feeds the indicator block holding AroonOscillator, and the previous-value block takes the same output one candle back.
- Four comparison blocks build the two crossings: the previous reading against a level and the current reading against the same level.
- Two more comparison blocks test the position against a zero constant, and each logical AND joins three conditions into one signal.
- Both position modify blocks send market orders and take their volume from a single shared constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
