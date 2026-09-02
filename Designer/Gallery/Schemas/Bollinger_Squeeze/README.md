# Bollinger Squeeze Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A breakout diagram built on Bollinger Bands: the bands are drawn one and eight tenths of a standard deviation around a twenty-period average, and a close outside them is taken as the start of a move rather than as an excess to fade. The order volume always carries the open position with it, so every signal reverses the side instead of adding to it.

![schema](schema.svg)

## Strategy Overview

- Bollinger Bands are calculated on finished candles of a single instrument; only the upper and the lower band take part in the decisions.
- The diagram is a breakout, not a reversion: it buys strength above the upper band and sells weakness below the lower band, the opposite of the Bollinger_Bands example in this gallery.
- The volume of every order is the base volume plus the absolute value of the current position, so a signal against an open position closes it and opens the new side in one order.
- Despite the name, no squeeze filter is applied: the original C# strategy computes the relative band width but never uses it in a condition, and the diagram stays faithful to what the code actually does.

## Entry and Exit Rules

- **Long entry**: The candle closes above the upper Bollinger band and the position is not already long. The order buys the base volume plus the size of the open position, which opens a long from flat or reverses an existing short.
- **Short entry**: The candle closes below the lower Bollinger band and the position is not already short. The order sells the base volume plus the size of the open position, which opens a short from flat or reverses an existing long.
- **Exit**: There is no separate exit and no protection block: a position is only ever left when the price closes beyond the opposite band and the reversing order flips the side.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Bollinger Period | 20 | Number of candles the bands are averaged over. |
| Bollinger Width | 1.8 | Standard deviation multiplier that sets the distance of the bands from the middle line. |
| Volume | 1 | Base order volume, in lots; the position size is added on top of it. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the Bollinger Bands indicator block and a converter that reads the close price of the same candle.
- Two converters typed as indicator values pull the upper and the lower band out of the single indicator output.
- Two comparison blocks test the close against the bands, two more compare the position against a zero constant, and each logical AND joins one band condition with one position condition.
- A formula block computes the base volume plus the absolute position and feeds both position modify blocks, which is what turns each entry into a reversal.
- The ten-bar pause the original code keeps after every entry is not reproduced: the available blocks have no bar counter, so the position checks alone hold the frequency of trading down.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
