# Bullish Engulfing with SMA Filter Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

An engulfing candle says that the side which controlled the previous bar has just been overrun. On its own that happens far too often to be worth trading, so a simple moving average decides where the signal is taken: a bullish engulfing is only bought below the average, a bearish one is only sold above it. The average is also the target the trade is closed at.

![schema](schema.svg)

## Strategy Overview

- Two candle pattern indicator blocks carry the ready-made Bullish Engulfing and Bearish Engulfing patterns, so the shape is recognized without writing a formula.
- A simple moving average of the closing price splits the chart into a cheap half and an expensive half.
- The pattern is bought only in the cheap half and sold only in the expensive half, which turns the diagram into a mean reversion example rather than a breakout one.
- The position guard makes sure a pattern is acted on only when the diagram is flat.

## Entry and Exit Rules

- **Long entry**: The candle pattern block reports a bullish engulfing, the candle closed below the moving average and the position is flat. The order buys one lot and opens a long.
- **Short entry**: The candle pattern block reports a bearish engulfing, the candle closed above the moving average and the position is flat. The order sells one lot and opens a short.
- **Exit**: A long is closed once a candle closes above the moving average, a short once a candle closes below it, both through position modify blocks in close mode. The original strategy instead exits on the same side of the average it entered on and relies on a pause of several hundred bars to hold the trade in between; a bar counter has no block of its own here, so the exit is the return to the average, which is the closest rule that still trades sensibly.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that filters the patterns and closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds four branches: the two pattern indicators, the moving average and a converter that reads the closing price.
- Two comparison blocks put the closing price on one side of the moving average or the other; the same two signals serve both as entry filters and as exit triggers.
- The position block is compared against a zero constant, and the result guards both entries.
- Each logical AND joins a pattern, a filter and the position guard, and triggers a position modify block; both entry orders take their volume from one shared constant, while the two closing blocks need none.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
