# Supertrend Flip Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Supertrend draws a single line that sits below the price in an uptrend and above it in a downtrend, at a distance of several average true ranges from the median price. The diagram trades the moment the close steps over that line: it buys the step up, sells the step down, and holds the side until the next flip.

![schema](schema.svg)

## Strategy Overview

- The Supertrend indicator is built on finished candles; its ATR period sets how far the line stands from price and the multiplier scales that distance.
- A converter takes the close price of each candle, and a crossing block compares it with the Supertrend line, firing only on the bar where the two actually cross.
- The strategy is always in the market after the first signal: there is no stop and no target, only the flip of the line.

## Entry and Exit Rules

- **Long entry**: The close crosses above the Supertrend line and the position is not already long. The order buys Volume plus the absolute value of the current position, which opens a long from flat or turns a short straight into a long.
- **Short entry**: The close crosses below the Supertrend line and the position is not already short. The order sells Volume plus the absolute value of the current position, which opens a short from flat or turns a long straight into a short.
- **Exit**: There is no separate exit and no protective stop: the position is left only by the opposite flip of the line, which reverses it in a single order.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| ATR period | 10 | ATR period the Supertrend line is built on. |
| ATR multiplier | 3 | Multiplier applied to the ATR, which sets how far the line stands from the median price. |
| Volume | 1 | Base order volume, in lots; the open position is added to it on a reversal. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the indicator block holding Supertrend and, through a converter, supplies the close price of the same candle.
- Both go into the crossing block, whose output is the long signal, while a logical NOT of it is the short signal.
- Each signal is joined by a logical AND with a comparison of the position against zero, so an entry is never added to a position already held on that side.
- A formula block computes Volume plus the absolute position and feeds the volume input of both position modify blocks, which is how one market order reverses the position.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
