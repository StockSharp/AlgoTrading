# Last Price Mean Reversion with Level1 Limit Orders
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This example is primarily about Order registering and Level1 execution: it expresses the familiar EMA-deviation mean reversion as marketable limit orders. Unlike the historical strategy name, every decision still comes from a finished four-hour candle; there is no tick-driven signal path.

![schema](schema.svg)

## Strategy Overview

- Finished four-hour closes feed EMA(20) and are compared with boundaries 0.5% below and above it.
- While flat, a close below the lower boundary requests a buy and a close above the upper boundary requests a sell.
- A long exits when the close returns to or above EMA; a short exits when it returns to or below EMA.
- Level1 best ask is sampled at each candle for buy limits and best bid for sell limits; positive-price gates prevent registration before a quote exists.
- The same one-unit volume feeds entries and exits, so a position opened by this diagram is flattened by one opposite fill.

## Entry and Exit Rules

- **Long entry**: With Position == 0 and Close < EMA × (1 − 0.5/100), register a buy limit at the sampled best ask. The through-spread price is intended to execute like the source's BuyMarket call.
- **Short entry**: With Position == 0 and Close > EMA × (1 + 0.5/100), register a sell limit at the sampled best bid. The through-spread price is intended to execute like SellMarket.
- **Exit**: For Position > 0 and Close >= EMA, the shared sell block places one unit at best bid. For Position < 0 and Close <= EMA, the shared buy block places one unit at best ask.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Time Frame | 04:00:00 | Finished candle interval used for EMA and every trading decision, matching the C# default. |
| EMA Period | 20 | Number of finished closes in ExponentialMovingAverage. |
| Entry Distance, % | 0.5 | Percentage distance from EMA required for a flat-position entry. |
| Shared Entry/Exit Volume | 1 | Single volume used by both entry and exit limit orders. |

## Diagram Details

- The C# strategy uses market orders for both entry and exit. The diagram deliberately keeps Order registering visible and uses a marketable limit—buy at best ask, sell at best bid—to obtain equivalent immediate execution.
- A passive variant would buy at best bid and sell at best ask, but then it must add Order cancellation or replacement for limits left working after the signal changes.
- Close and EMA arrive from the same finished candle. A small variable releases the cached close only after the EMA update, preventing a comparison between the new close and the previous EMA.
- The shared volume mirrors parameterless BuyMarket/SellMarket using Strategy.Volume. If an external or differently sized position is present, a fixed one-unit exit cannot be assumed to flatten it.
- The trading idea overlaps the published MA_Deviation example. The distinct lesson here is explicit Level1 quote sampling and marketable limit-order execution.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
