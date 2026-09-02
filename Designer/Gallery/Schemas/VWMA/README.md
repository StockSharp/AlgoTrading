# VWMA Price Cross Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The Volume Weighted Moving Average weights every price by the volume traded at it, so it leans towards the levels where money actually changed hands. The diagram follows the close price across that average: a close moving from below the line to above it buys, a close moving the other way sells. The original strategy uses one-minute candles and sits out a number of bars after each trade; the diagram works on five-minute candles and leaves that pause out, because the position guard already prevents a second entry in the same direction.

![schema](schema.svg)

## Strategy Overview

- VolumeWeightedMovingAverage receives the whole candle, not just a price, because it needs the traded volume as well.
- Both the close price and the average are also kept one candle back, so the crossing is read exactly the way the original code reads it.
- Every entry is guarded by the position: a buy only goes out while the position is not long, a sell only while it is not short.
- The cooldown of the original strategy is not reproduced, so the diagram answers every crossing it sees.

## Entry and Exit Rules

- **Long entry**: The previous close was at or below the previous VWMA and the current close is above the current VWMA, while the position is not long. The order buys one lot, which opens a long from flat or closes an existing short.
- **Short entry**: The previous close was at or above the previous VWMA and the current close is below the current VWMA, while the position is not short. The order sells one lot, which opens a short from flat or closes an existing long.
- **Exit**: There is no dedicated exit block and no protective stop: the opposite crossing flattens the position, because every order carries the same volume.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| VWMA Length | 14 | Averaging length of the Volume Weighted Moving Average. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on; the original strategy used one minute. |

## Diagram Details

- The candle block feeds two things at once: the indicator block with VolumeWeightedMovingAverage and a converter that reads the close price.
- Two previous-value blocks hold the close and the average of the preceding candle.
- Four comparison blocks assemble the two crossings, two more compare the position with a zero constant, and each logical AND joins three of those signals.
- Both position modify blocks send market orders sized from one shared volume constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
