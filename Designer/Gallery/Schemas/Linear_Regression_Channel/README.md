# Linear Regression Channel Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A least-squares line is fitted through the last fifty closes and a channel is drawn around it at a multiple of the regression's standard error. Price outside the channel is treated as a stretched move, and the strategy fades it back towards the line as long as the slope of the channel is on its side.

![schema](schema.svg)

## Strategy Overview

- LinearReg gives the value of the fitted line on the current bar, LinearRegSlope gives its direction, and StandardError measures how far the closes usually scatter around it.
- The two bands are the line plus and minus the deviation multiplier times the standard error, so the channel widens and narrows with the market on its own.
- The slope acts as a filter: a dip is only bought inside a rising channel, a spike is only sold inside a falling one.
- The regression line is the profit target; there is no stop-loss or take-profit, exactly as in the source strategy.

## Entry and Exit Rules

- **Long entry**: The regression slope is above zero, the close is below the lower band, and the position is flat. The buy order opens a long of one lot.
- **Short entry**: The regression slope is below zero, the close is above the upper band, and the position is flat. The sell order opens a short of one lot.
- **Exit**: A long is closed as soon as the close reaches the regression line from below, a short as soon as it reaches it from above. Both exit blocks run in close-position mode, so they do nothing when there is no position to close.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| LinearReg Length | 50 | Number of candles the regression line is fitted over. |
| LinearRegSlope Length | 50 | Number of candles the slope is measured over; keep it equal to the line length. |
| StandardError Length | 50 | Number of candles the standard error is measured over; keep it equal to the line length. |
| Channel Deviation | 1.5 | Channel half-width in standard errors of the regression. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- One candle block feeds three indicator blocks and a converter for the close price, so every value in the diagram comes from the same finished candle.
- Two formula blocks build the bands out of the line, the standard error and a shared deviation constant that can be optimized.
- Six comparison blocks turn those numbers into flags: two for the slope, two for the bands and two for the return to the line.
- Each entry is a logical AND of slope, band and a flat position; the exits are wired straight from their comparison to a close-position block.
- The original strategy waits twenty bars between trades and computes the deviation over the whole window, while StandardError divides by the window minus two, which makes the channel about two percent wider; lower the deviation to about 1.47 if you want the exact original band.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
