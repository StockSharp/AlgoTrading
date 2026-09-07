# Armed Price Trigger OCO Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram maintains two virtual breakout triggers around a Donchian channel calculated from finished five-minute candles. Live best ask and best bid prices are evaluated against the latest channel boundaries; the first eligible side opens a market position, which is then managed by percentage-based take-profit and stop-loss protection.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed Donchian Channels(20), producing the upper and lower breakout boundaries.
- Each market-depth update samples the latest formed channel values and reads `BestAsk.Price` and `BestBid.Price` from the order book.
- An upper breakout can buy and a lower breakout can sell only while `Armed` is enabled and the position is flat.
- A Flag block passes the first valid pulse on each side and suppresses repeats until a position transition resets both one-shot gates.
- Position protection closes the filled entry at a 1% take-profit or a 0.6% stop-loss, while the chart displays candles, both channel boundaries and all fills.

## Entry and Exit Rules

- **Long entry**: When `Armed` is true, the position is flat, and the best ask is greater than or equal to the latest upper Donchian boundary, submit a market buy for `Volume`.
- **Short entry**: When `Armed` is true, the position is flat, and the best bid is less than or equal to the latest lower Donchian boundary, submit a market sell for `Volume`.
- **Exit**: Close the open position when the live order book reaches either the 1% profit target or the 0.6% loss threshold measured from the entry fill.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Channel Length | 20 | Number of finished candles used by Donchian Channels. |
| Armed | true | Master switch that enables both breakout entry routes. |
| Take Profit, % | 1 | Percentage distance from the entry fill to the profit target. |
| Stop Loss, % | 0.6 | Percentage distance from the entry fill to the loss threshold. |
| Volume | 1 | Market-order volume used for either entry direction. |
| Candles | 00:05:00 | Time frame of the finished candles used to calculate the channel. |

## Diagram Details

- UpperBand and LowerBand converters extract both Donchian boundaries. Two Variable blocks retain them and emit their latest values in the causal context of each market-depth update.
- Before Donchian Channels(20) is formed, the boundary variables have no stored value and the entry comparisons remain inactive.
- The order-book converter paths `BestAsk.Price` and `BestBid.Price` provide the live prices used by the two comparisons.
- Each entry gate combines three Boolean inputs: the relevant price comparison, `position = 0`, and the exposed `Armed` value.
- The two Flag blocks implement one-shot virtual OCO behavior without resting exchange orders. After one side fills, the non-flat position blocks both entry routes until protection closes it.
- Both Modify position blocks use market orders. Their entry fills feed Position protection directly, and market depth supplies the prices used to evaluate its exit levels.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
