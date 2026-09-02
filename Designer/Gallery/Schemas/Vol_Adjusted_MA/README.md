# Volatility Adjusted Moving Average Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The diagram wraps a Simple Moving Average in a channel whose half-width is a multiple of the Average True Range, so the edges move apart when the market gets nervous and close in when it calms down. A close beyond an edge is treated as a real breakout, and the trade is handed back as soon as the price returns to the average.

![schema](schema.svg)

## Strategy Overview

- A SimpleMovingAverage draws the centre line and an AverageTrueRange decides how far the edges sit from it, which makes the channel adapt to the current volatility.
- Two formula blocks assemble the edges as SMA + multiplier * ATR and SMA - multiplier * ATR from the same three sources.
- Entries are taken only from a flat position, and the only way out is the close coming back through the centre line; there is no stop loss or take profit, exactly as in the C# original.
- Two departures from the original: the 500-bar pause after every trade is not reproduced, so the diagram trades more often, and the working candle is five minutes instead of one, which is what the packaged history provides.

## Entry and Exit Rules

- **Long entry**: The close is above the upper edge SMA + multiplier * ATR while the position is flat. The modify block buys the shared volume at market.
- **Short entry**: The close is below the lower edge SMA - multiplier * ATR while the position is flat. The modify block sells the shared volume at market.
- **Exit**: A long is given back on the first candle that closes below the SMA and a short on the first candle that closes above it; the close-position blocks act only when there is something to close.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Length of the Simple Moving Average that forms the centre line and the exit level. |
| ATR Length | 14 | Length of the Average True Range that measures the current volatility. |
| ATR multiplier | 2 | How many ATRs the channel edges sit away from the centre line. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both indicators and a converter that pulls the close price out of the candle.
- Two formula blocks combine the moving average, the range and the multiplier constant into the upper and lower edges.
- Four comparison blocks build the signals: two against the channel edges for the entries, two against the centre line for the exits.
- The position block, compared with a zero constant, joins every signal through a logical AND, so no order is ever added to a position that is already open.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
