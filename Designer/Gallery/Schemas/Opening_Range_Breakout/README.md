# Opening Range Breakout (Bollinger Breakout with EMA Filter) Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The example keeps the name of the original strategy, but there is no session opening range in it: what it actually trades is a breakout of the Bollinger Bands confirmed by a slow EMA. Price leaving the band is the trigger, the EMA decides whether that break is going with the market or against it, and the middle band brings the trade home.

![schema](schema.svg)

## Strategy Overview

- Bollinger Bands and a 50-period EMA are calculated on the same half-hour candles, and every decision uses the closing price of a finished candle.
- A breakout counts only in the direction of the trend: above the upper band the close must also be above the EMA, below the lower band it must also be below it.
- The middle band of the Bollinger Bands is the exit for both sides, so the trade lasts exactly as long as the price stays away from its own average. There is no stop-loss and no take-profit.

## Entry and Exit Rules

- **Long entry**: The candle closes above the upper Bollinger band, that close is also above the EMA and the position is flat. The modify block buys the shared volume at market.
- **Short entry**: The candle closes below the lower Bollinger band, that close is also below the EMA and the position is flat. The modify block sells the shared volume at market.
- **Exit**: A long is closed by the first close below the middle band and a short by the first close above it; both closing blocks work in close-position mode, so they act only when there is something to close.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Bollinger Length | 20 | Averaging length of the Bollinger Bands, which is also the length of the middle band. |
| Bollinger Width | 2 | Band width in standard deviations; the original code fixes it at two. |
| EMA Length | 50 | Length of the EMA that decides the direction a breakout is allowed to be traded in. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:30:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the Bollinger Bands, the EMA and a converter for the closing price; three more converters split the bands into upper, lower and middle lines.
- Six comparisons cover the whole logic: two for the bands, two for the EMA filter and two for the return to the middle band.
- Both entry AND blocks require a flat position, so an entry never adds to an open trade; the closing blocks are wired straight to the middle-band comparisons.
- Two things from the C# original are missing here: the 10-bar pause between actions, which has no block in the Designer, and the immediate reversal — this diagram closes at the middle band first and opens the opposite side on a later candle.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
