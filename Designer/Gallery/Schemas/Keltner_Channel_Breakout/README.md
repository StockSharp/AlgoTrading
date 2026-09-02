# Keltner Channel Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A Keltner channel is an exponential moving average with edges pushed out by a multiple of the Average True Range. The diagram waits for a close to step outside an edge that the previous close was still inside of, and turns the whole position around in the direction of the break. There is no stop and no target: the opposite breakout is what takes the trade off.

![schema](schema.svg)

## Strategy Overview

- KeltnerChannels produces the channel in one block, and two converters pull the upper and the lower edge out of its value.
- Previous-value blocks hold the two edges and the close from one bar back, so the break is measured against the level the market already saw rather than against an edge that moved with the same candle.
- Each order carries the shared volume plus the absolute position, so one order reverses the trade instead of only shrinking it.
- The C# original runs a 500-period channel with a multiplier of 10 on one-minute candles; the diagram uses the 20 / 2 channel documented in its README on five-minute candles, so a breakout actually happens on ordinary data.

## Entry and Exit Rules

- **Long entry**: The close is above the previous candle's upper band while the previous close was still at or below it, and the position is not long. The order buys the volume plus whatever short is open, which reverses it into a long.
- **Short entry**: The close is below the previous candle's lower band while the previous close was still at or above it, and the position is not short. The order sells the volume plus whatever long is open, which reverses it into a short.
- **Exit**: There is no exit block: the opposite breakout reverses the position, exactly as in the original strategy, which has neither stop loss nor take profit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Channel period | 20 | Period of the Keltner channel; it sets both the moving average and the range the width is built from. |
| ATR multiplier | 2 | How many ranges the edges of the channel sit away from the middle line. |
| Volume | 1 | Order volume, in lots, before the open position is added to it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the indicator and a converter that reads the close price out of the candle.
- Three previous-value blocks shift the upper band, the lower band and the close by one bar; the indicator only emits once it is formed, so the first bars are skipped by themselves.
- Four comparison blocks form each side of the break: one for the candle that steps out and one for the candle that was still inside.
- The position is compared with a zero constant and joins both logical ANDs, while a formula block adds its absolute value to the shared volume constant to size the reversing order.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
