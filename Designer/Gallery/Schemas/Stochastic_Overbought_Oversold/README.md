# Stochastic Overbought/Oversold Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The stochastic %K line measures where the close sits inside the recent high-low range, and this diagram fades the extremes of that range. What matters is the moment %K enters a zone, not the whole stretch it spends there, so a previous-value block turns the level test into a level crossing and one signal produces one order.

![schema](schema.svg)

## Strategy Overview

- The %K line is calculated on finished candles of a single instrument; the smoothed %D line takes no part in the decision, exactly as in the original strategy.
- A three-candle window makes %K a very fast line: it reaches both zones often, which is what gives this example its trade count.
- The oversold and overbought levels are constants of the diagram, so they can be edited and optimized; in the original code they are fixed at 20 and 80.
- Every order uses the same volume, so a signal against an open position closes it instead of reversing into a larger one.

## Entry and Exit Rules

- **Long entry**: The previous %K reading was at or above the oversold level, the current reading is below it, and the position is not long. The order buys one lot, which opens a long from flat or closes an existing short.
- **Short entry**: The previous %K reading was at or below the overbought level, the current reading is above it, and the position is not short. The order sells one lot, which opens a short from flat or closes an existing long.
- **Exit**: There is no separate exit block: the opposite level crossing closes the position, because every order carries the same volume. The original strategy also pauses for a fixed number of candles after a trade; a bar counter has no block of its own, so the level crossing takes over that role and keeps the diagram from firing on every candle inside a zone.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| %K Length | 3 | Window of the highest high and the lowest low the %K line is measured against. |
| Oversold | 20 | Level the %K line has to cross downwards for a buy. |
| Overbought | 80 | Level the %K line has to cross upwards for a sell. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the %K indicator block, whose output goes both into the comparison blocks and into a previous-value block.
- Four comparison blocks build the two crossings: the previous reading against a level and the current reading against the same level.
- The position block is compared against a zero constant twice, giving a not-long guard for the buy side and a not-short guard for the sell side.
- Each logical AND joins the two halves of a crossing with its position guard and triggers a position modify block; both modify blocks take their volume from one shared constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
