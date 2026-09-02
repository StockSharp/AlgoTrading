# MA + Parabolic SAR Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A Simple Moving Average says which side of the market is worth trading and a Parabolic SAR says when: the diagram waits for the close to cross the SAR line in the direction the average already points to. The opposite cross of the same line hands the position back, so the strategy is either riding a trend or waiting for the next one.

![schema](schema.svg)

## Strategy Overview

- SimpleMovingAverage is the direction filter; longs are taken only while the close is above it, shorts only while it is below.
- ParabolicSar supplies the timing, and a single crossing block turns the close crossing that line into one pulse: true for an upward cross, false for a downward one.
- Entries are guarded by the current position, and the exits use close-position blocks, which act only when there is a position of the right sign to close.
- Two departures from the C# original: it builds its SAR substitute from a fast EMA and never reads the declared SAR settings, while the diagram uses a real ParabolicSar; and the 20-bar pause between entries is not reproduced.

## Entry and Exit Rules

- **Long entry**: The close crosses the ParabolicSar line upwards while it is above the SMA and the position is not long. The modify block buys the shared volume at market.
- **Short entry**: The close crosses the ParabolicSar line downwards while it is below the SMA and the position is not short. The modify block sells the shared volume at market.
- **Exit**: A long is closed on the first downward cross of the SAR line and a short on the first upward cross, without asking the moving average; there is no stop loss or take profit, as in the original strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Length of the Simple Moving Average that decides the trend direction. |
| SAR Acceleration | 0.02 | Starting acceleration factor of the Parabolic SAR. |
| SAR Max acceleration | 0.2 | Ceiling the acceleration factor of the Parabolic SAR grows to. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both indicators and a converter that reads the close price out of the candle.
- The crossing block compares the close with the SAR line; a logical NOT turns its output into the downward cross used by the short entry and the long exit.
- Comparison blocks test the close against the SMA and the position against a zero constant, and four logical ANDs combine them into the entry and exit signals.
- Two modify blocks open positions with the shared volume constant, and two more close them with the close-position condition.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
