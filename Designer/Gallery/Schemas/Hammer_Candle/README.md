# Hammer / Inverted Hammer with SMA Filter Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A hammer is a candle with a small body, a long lower shadow and almost no upper one: price was pushed far down inside the bar and bought back before the close. The inverted hammer is its mirror image. On their own these shapes appear everywhere, so a simple moving average decides where they are worth taking: a hammer is only bought below the average, an inverted hammer is only sold above it.

![schema](schema.svg)

## Strategy Overview

- Two candle pattern blocks carry the exact formulas of the original strategy: a body greater than zero, one shadow longer than twice the body and the opposite shadow shorter than half the body.
- The built-in Hammer and Inverted Hammer patterns are deliberately not used, because their formulas measure the shadows against the candle length rather than against the body.
- A simple moving average of the closing price splits the chart into a cheap half and an expensive half and is both the entry filter and the exit line.
- The position guard makes sure a pattern is acted on only when the diagram is flat.

## Entry and Exit Rules

- **Long entry**: The candle pattern block reports a hammer, the candle closed below the moving average and the position is flat. The order buys one lot and opens a long.
- **Short entry**: The candle pattern block reports an inverted hammer, the candle closed above the moving average and the position is flat. The order sells one lot and opens a short.
- **Exit**: A long is closed once a candle closes above the moving average, a short once a candle closes below it, both through position modify blocks in close mode. The original strategy instead exits on the same side of the average it entered on and relies on a pause of several hundred bars to hold the trade in between; a bar counter has no block of its own here, so keeping that exit literally would close every trade on the very next candle. The return to the average is the closest rule that still holds a position for a sensible stretch.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that filters the patterns and closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both pattern blocks, the moving average and a converter that pulls the closing price out of the candle.
- Two comparison blocks put that close against the average and are reused twice each: as the entry filter of one side and as the exit trigger of the other.
- The position block is compared with a zero constant, and each logical AND joins the pattern, the side of the average and that guard.
- Both entry blocks send market orders and take their volume from one shared constant; the two exit blocks work in close mode and only act when there is something to close.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
