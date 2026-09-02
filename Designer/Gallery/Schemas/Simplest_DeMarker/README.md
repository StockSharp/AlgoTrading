# Simplest DeMarker Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

DeMarker measures how far each candle reaches beyond the previous one, upwards against downwards, and returns a value between 0 and 1. This diagram does not buy the extreme, it buys the way back from it: a reading that climbs from below the oversold level up to it buys, a reading that falls from above the overbought level back to it sells. The original strategy runs on hourly candles and waits four candles between trades; the diagram uses five-minute candles and leaves the pause out, since the position guard already blocks a second entry in the same direction.

![schema](schema.svg)

## Strategy Overview

- DeMarker is calculated on finished candles of a single instrument and lives entirely between 0 and 1, with 0.5 as the neutral middle.
- A previous-value block holds the reading of the candle before, so the diagram reacts to the return into the neutral zone rather than to sitting inside it.
- The current position joins both decisions: a buy is only sent while the position is not long and a sell only while it is not short.
- The four-candle cooldown of the original is not reproduced; it can be added later without touching the signal part of the diagram.

## Entry and Exit Rules

- **Long entry**: The previous DeMarker reading was below the oversold level, the current one is at or above it, and the position is not long. The order buys one lot, which opens a long from flat or closes an existing short.
- **Short entry**: The previous DeMarker reading was above the overbought level, the current one is at or below it, and the position is not short. The order sells one lot, which opens a short from flat or closes an existing long.
- **Exit**: There is no exit block and no protective stop, exactly as in the original strategy: the opposite signal flattens the position, because every order carries the same volume.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| DeMarker Length | 14 | Averaging length of the DeMarker oscillator. |
| Oversold | 0.2 | Oversold level; returning up to it is the buy signal. |
| Overbought | 0.8 | Overbought level; returning down to it is the sell signal. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on; the original strategy used one hour. |

## Diagram Details

- The candle block feeds the indicator block with DeMarker, and the previous-value block takes the same output one candle back.
- Four comparison blocks build the two returns: the previous reading beyond a level and the current reading back at it.
- Two more comparison blocks test the position against a zero constant, and each logical AND joins three conditions into one signal.
- Both position modify blocks send market orders and take their volume from a single shared constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
