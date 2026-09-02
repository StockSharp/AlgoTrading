# MA + ADX Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A trend diagram with a strength filter. The ExponentialMovingAverage says which side of the market to be on, the directional index DX says whether the move deserves a position at all, and the position is given up as soon as the close returns to the other side of the average.

![schema](schema.svg)

## Strategy Overview

- The candle close is compared against an ExponentialMovingAverage; above the average means long, below it means short.
- DirectionalIndex delivers the DX value, the same formula the original strategy computes by hand from +DM and -DM, and an entry is allowed only while DX is above the threshold.
- Entries are taken from a flat position only, and each exit closes exactly the open position, so the diagram never pyramids.
- The exit ignores trend strength: once the close is back on the other side of the average, the position goes, no matter what DX says.

## Entry and Exit Rules

- **Long entry**: The close is above the EMA, DX is above the trend strength threshold and the position is flat. The order buys the base volume and opens a long.
- **Short entry**: The close is below the EMA, DX is above the trend strength threshold and the position is flat. The order sells the base volume and opens a short.
- **Exit**: A long is closed as soon as a candle closes below the EMA, a short as soon as a candle closes above it; the closing blocks take their volume from the open position. The original strategy has no stop loss or take profit, and its pause of a hundred candles after each trade is not reproduced here, so this diagram trades more often than the source.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| EMA Length | 20 | Length of the ExponentialMovingAverage that sets the direction. |
| DX Length | 14 | Length of the directional index that measures trend strength. |
| Trend Strength | 25 | Value of DX above which a new position is allowed. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the two indicators and a converter that extracts the close price.
- Two comparison blocks place the close relative to the EMA and are reused: the same signal opens one side and closes the other.
- The position block feeds three comparisons against zero: flat guards both entries, long and short guard the two exits.
- The entry blocks work with the open-position condition and take their volume from a shared constant; the exit blocks use the close-position condition and compute the volume themselves.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
