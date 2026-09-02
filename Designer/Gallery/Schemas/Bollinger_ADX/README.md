# Bollinger Bands and ADX Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A breakout is only worth taking when the market is actually going somewhere. This diagram waits for a close outside a Bollinger band, which says the move is unusually large for recent volatility, and asks ADX whether a trend is behind it. If both agree, the position is opened in the direction of the break and it is given up as soon as price falls back to the middle band.

![schema](schema.svg)

## Strategy Overview

- Bollinger Bands are calculated on finished candles of one instrument; the upper and lower bands mark the breakout levels and the middle band, which is the moving average of the same length, marks the exit.
- ADX measures trend strength without saying anything about direction, so it is used purely as a filter: below the threshold every breakout is ignored.
- The current position takes part in both entries, and the two closing blocks are set to close a position rather than open one, so each of them can only act on the side it belongs to.
- The source strategy blocks itself for a hundred bars after any trade, exits included. That counter has no equivalent among the blocks, so the diagram leaves it out; the exit at the middle band therefore always works, which is the more sensible behaviour anyway.

## Entry and Exit Rules

- **Long entry**: The close is above the upper Bollinger band, ADX is above its threshold and the position is flat. One lot is bought at market.
- **Short entry**: The close is below the lower Bollinger band, ADX is above its threshold and the position is flat. One lot is sold at market.
- **Exit**: A long is closed on the first close below the middle band and a short on the first close above it. There is no stop loss or take profit, exactly as in the source strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Bollinger Length | 20 | Averaging length of the Bollinger Bands and of their middle line. |
| Bollinger Width | 2.0 | Standard deviation multiplier that sets the width of the bands. |
| ADX Length | 14 | Length of the Average Directional Index. |
| ADX Threshold | 25 | Level above which ADX is considered strong enough to trade the breakout. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds two indicator blocks and a converter for the close price; three more converters pull the upper band, the lower band and the middle band out of the single Bollinger value, and one pulls the ADX line out of its own.
- Five comparison blocks do the work: two for the breakout, two for the return to the middle band and one for the trend filter against a threshold constant.
- Each logical AND joins one breakout condition, the trend filter and the position check, then triggers a position modify block that opens a position and takes its volume from the shared constant.
- The two exit comparisons drive position modify blocks set to close, which need no volume of their own because the block closes whatever is open.
- The original code computes trend strength by hand as an unsmoothed DX. The diagram uses the standard ADX instead, which is the Wilder-smoothed version of the same figure, so the moments the threshold is crossed differ slightly.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
