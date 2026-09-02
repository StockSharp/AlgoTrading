# ADX Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Most diagrams compare an indicator with a fixed level. This one compares the Average Directional Index with itself: a simple moving average of the ADX line is the centre, a band is built around it out of the current distance between the two, and a break of that band is read as a sudden burst of trend strength. The direction is taken from the candle that produced it — a candle closing above its open buys, any other sells.

![schema](schema.svg)

## Strategy Overview

- The ADX line of the Average Directional Index is the only input of the whole construction; the +DI and -DI lines are not used.
- That line feeds a second indicator block, a twenty-period simple moving average, so the diagram runs an indicator on an indicator.
- A formula block builds the band as the average plus the multiplier times twice the absolute distance between the ADX and its average, exactly as the original code computes it.
- Entries reverse an open position in one order, because the order volume is the shared volume plus whatever is already held.

## Entry and Exit Rules

- **Long entry**: The ADX line is above the band, the candle closed above its open and the position is not long. The order buys the shared volume plus the size of an open short, so one market order closes the short and opens the long.
- **Short entry**: The ADX line is above the band, the candle closed at or below its open and the position is not short. The order sells the shared volume plus the size of an open long.
- **Exit**: The position is closed as soon as the ADX line drops back below its own moving average, a long by a sell in close mode and a short by a buy in close mode. On top of that a position protection block carries the two percent stop loss of the original; the original's take profit is set to zero, that is disabled, so no target is wired here either. One thing is worth knowing before optimizing: while the multiplier stays below 0.5 the band condition is algebraically the same as 'ADX above its average', so at the default of 0.1 the band adds nothing and the diagram reads simply as ADX crossing its own average up and down. The multiplier is kept as a constant so that larger values behave exactly like the original.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| ADX Length | 14 | Averaging length of the Average Directional Index. |
| Average Length | 20 | Length of the simple moving average that smooths the ADX line. |
| Multiplier | 0.1 | Multiplier of the band width; below 0.5 the band collapses onto the moving average itself. |
| Stop Loss % | 2 | Stop loss distance from the entry price, in percent. |
| Volume | 1 | Order volume, in lots, before the open position is added to it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the ADX indicator and two converters that read the open and the close of the candle.
- A converter pulls the ADX line out of the complex indicator value and passes it both to the moving average block and to the comparisons.
- One formula block computes the whole band in a single expression, which keeps the arithmetic of the original in one readable place instead of a chain of small blocks.
- A second formula block adds the absolute position to the shared volume, and the two exits are triggered straight from the 'ADX below its average' comparison, so they only act when there is something to close.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
