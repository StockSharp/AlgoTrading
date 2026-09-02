# OBV Channel Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

On-Balance Volume adds the volume of every up candle and subtracts the volume of every down candle, so its curve is the running balance of buying against selling pressure. This diagram puts a Donchian-style channel on that curve instead of on price: when OBV leaves the channel of the previous candles upwards, accumulation has taken over and the schema buys; when it leaves downwards, distribution has taken over and the schema sells.

![schema](schema.svg)

## Strategy Overview

- The channel is built by a Highest and a Lowest block of 60 values, both fed by the On-Balance Volume block rather than by candles.
- Two previous-value blocks hold the channel of the preceding candle, so the breakout is measured against a border that the current OBV value has not yet moved.
- Because the border comes from the previous candle, a break is an event and not a state: the very candle that pushes OBV past the old extreme is the one that trades.
- The original strategy is named after ATR, but its own code never uses that indicator, so the diagram leaves it out and keeps only what actually decides a trade.

## Entry and Exit Rules

- **Long entry**: The current OBV value is above the channel top of the previous candle and the position is not long. The order buys one lot, which opens a long from flat or closes an existing short.
- **Short entry**: The current OBV value is below the channel bottom of the previous candle and the position is not short. The order sells one lot, which opens a short from flat or closes an existing long.
- **Exit**: The protection block closes the trade at a take profit of 5 percent or a stop loss of 3 percent from the entry price; an opposite breakout also flattens the position, because every order uses the same volume.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Channel Length | 60 | Number of OBV values in the Highest and Lowest window; set both blocks to the same length. |
| Take profit, % | 5 | Take profit distance from the entry price, in percent. |
| Stop loss, % | 3 | Stop loss distance from the entry price, in percent. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the On-Balance Volume block, whose output goes on to the Highest and the Lowest block, an indicator reading another indicator.
- Each channel border passes through a previous-value block, so the comparison uses the border of the candle before the breakout.
- Two comparison blocks test the current OBV against those borders, and two more test the position against a zero constant; each logical AND joins a breakout with its position guard.
- The original keeps a sticky bull or bear regime and trades only when the regime flips; the diagram gets the same single entry per swing from the position guard, which blocks a repeated breakout in the direction it is already positioned.
- Both modify blocks send market orders with the volume of one shared constant, and their own trades feed the protection block with the take profit and the stop loss.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
