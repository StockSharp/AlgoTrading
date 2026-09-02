# Bollinger Bands + RSI Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two classic tools answer two different questions here. Bollinger Bands say how far price has travelled from its own average, and the Relative Strength Index says whether the move behind that distance is exhausted. A trade is taken only when both agree, and it is given up as soon as price is back at the middle band.

![schema](schema.svg)

## Strategy Overview

- Bollinger Bands and the Relative Strength Index are calculated on finished candles of a single instrument.
- The bands supply three numbers to the diagram at once: the upper band, the lower band and the middle moving average.
- An entry needs a closing price outside a band and an RSI reading in the matching extreme zone; one condition alone is never enough.
- The middle band is the target: crossing back to it closes the position, so the diagram never holds a trade that has already reverted.

## Entry and Exit Rules

- **Long entry**: The candle closes below the lower Bollinger band, RSI is below the oversold level and the position is flat. The order buys one lot and opens a long.
- **Short entry**: The candle closes above the upper Bollinger band, RSI is above the overbought level and the position is flat. The order sells one lot and opens a short.
- **Exit**: A long is closed when the close returns above the middle band, a short when the close returns below it. Both exits use position modify blocks in close mode, so they fire only when there is a position of the matching side and no protective stop is involved.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Bollinger Length | 20 | Averaging length of the Bollinger Bands. |
| Bollinger Width | 2 | Standard deviation multiplier that sets the band width. |
| RSI Length | 14 | Averaging length of the Relative Strength Index. |
| RSI Oversold | 30 | Level below which RSI is treated as oversold. |
| RSI Overbought | 70 | Level above which RSI is treated as overbought. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds three branches: the Bollinger block, the RSI block and a converter that reads the closing price.
- Three converter blocks split the Bollinger value into the upper band, the lower band and the middle moving average.
- Six comparison blocks build the conditions: close against each band, RSI against each level, and the position against a zero constant.
- Each logical AND joins a band condition, an RSI condition and the position guard, and triggers a position modify block whose volume comes from a shared constant.
- The original strategy pauses for a fixed number of bars after every trade; a bar counter has no block of its own, so the pause is left out and the middle band alone decides when a trade ends.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
