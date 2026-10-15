# Ichimoku Kumo Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The name comes from the Ichimoku cloud, but the strategy behind this diagram trades the fastest pair of lines instead: Tenkan-sen against Kijun-sen. Both are midpoints of the highest high and the lowest low over their period, so their crossing is a compact trend signal, and the cloud is left out of the decision on purpose.

![schema](schema.svg)

## Strategy Overview

- One Ichimoku block builds all five lines; two converters take only Tenkan-sen and Kijun-sen out of it, and the cloud lines take no part in the rules.
- A crossing block fires only on the candle where Tenkan-sen actually crosses Kijun-sen, so a trend that simply lasts produces no repeated orders.
- Every entry is combined with the current position, which is what keeps the diagram from piling more lots onto a side it already holds.

## Entry and Exit Rules

- **Long entry**: Tenkan-sen crosses above Kijun-sen and the position is not long. The order buys the fixed volume, which opens a long from flat or closes an existing short.
- **Short entry**: Tenkan-sen crosses below Kijun-sen and the position is not short. The order sells the fixed volume, which opens a short from flat or closes an existing long.
- **Exit**: There is no separate exit block and no protective stop: because every order uses the same volume, the opposite crossing takes the position back to flat instead of reversing it, and the other side is only opened on the crossing after that.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Tenkan period | 9 | Period of Tenkan-sen, the midpoint of the highest high and the lowest low over that many candles. |
| Kijun period | 26 | Period of Kijun-sen, built the same way over a longer window. |
| Senkou Span B period | 52 | Period of Senkou Span B; it is not part of the rules and only affects how long the indicator needs to be fully formed. |
| Volume | 1 | Order volume, in lots; the same value is used for opening and for closing. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds a single Ichimoku indicator block, and two converters read the Tenkan and Kijun values out of the complex indicator value.
- Both lines meet in the crossing block, whose output is the long signal; a logical NOT of it gives the short signal.
- The position block is compared against a zero constant twice, which yields the guards Position <= 0 and Position >= 0.
- Each logical AND joins one crossing signal with one position guard and triggers a position modify block; both blocks send market orders and take their volume from a shared constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
