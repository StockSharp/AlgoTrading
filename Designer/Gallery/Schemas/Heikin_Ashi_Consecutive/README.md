# Heikin-Ashi Consecutive Candles Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heikin-Ashi candles average the noise away, so their colour stays the same for as long as a move really lasts. This diagram counts that persistence: seven bullish bodies in a row are treated as an established uptrend and bought, seven bearish bodies in a row are sold, and a percent stop loss limits what a false run can cost.

![schema](schema.svg)

## Strategy Overview

- A formula block builds the Heikin-Ashi body as the average of open, high, low and close minus the midpoint of the previous candle; a positive body is a bullish Heikin-Ashi candle, a negative one is bearish.
- The run of same-coloured candles is measured without a counter: the Lowest of the last seven bodies being above zero means all seven were bullish, and the Highest being below zero means all seven were bearish.
- An order is sized as volume plus the absolute position, so one order flips a short straight into a long and the other way round, exactly as the C# original does.
- The Heikin-Ashi open is defined by its own previous value, which a diagram cannot feed back into a block; the midpoint of the previous ordinary candle stands in for it, so the runs found here are close to, but not identical with, the ones the source code counts.

## Entry and Exit Rules

- **Long entry**: The Lowest of the last seven Heikin-Ashi bodies is above zero, meaning all seven candles were bullish, and the position is not already long. The order buys volume plus the absolute position, opening a long from flat or reversing a short.
- **Short entry**: The Highest of the last seven Heikin-Ashi bodies is below zero, meaning all seven candles were bearish, and the position is not already short. The order sells volume plus the absolute position, opening a short from flat or reversing a long.
- **Exit**: There is no exit rule of its own, as in the source strategy: a position is either reversed by the opposite run or stopped out by the position-protection block, which places a stop loss a fixed percentage away from the fill price. There is no take profit and no trailing.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Consecutive candles | 7 | How many same-coloured Heikin-Ashi candles in a row make a signal; it is the length of both the Lowest and the Highest block. |
| Stop loss, % | 2 | Distance of the stop loss from the entry price, in percent. |
| Volume | 1 | Base order volume, in lots; the absolute position is added on top so that a reversal happens in one order. |
| Candles | 00:30:00 | Candle time frame the whole diagram works on, the same half hour the original strategy uses. |

## Diagram Details

- The candle block feeds four converters for open, high, low and close, and two previous-value blocks hand the formula the candle before.
- The formula output goes into a Lowest and a Highest block of the same length, and two comparisons against a zero constant turn them into the two run conditions.
- The position block, compared with zero twice, joins each run condition through a logical AND, so no order is added to a position that already points the right way.
- Both modify blocks take their size from a formula that adds the absolute position to the shared volume, and their fills feed the position-protection block that carries the stop loss.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
