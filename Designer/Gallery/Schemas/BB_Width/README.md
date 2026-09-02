# Bollinger Band Width Expansion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The signal is the distance between the two Bollinger bands, not the price touching them. A formula block subtracts the lower band from the upper one, and the result is held for one candle so the two readings can be compared. The moment the bands start opening the diagram takes a position, and the side is decided by nothing more than where the candle closed relative to the middle band.

![schema](schema.svg)

## Strategy Overview

- Bollinger Bands supply three lines at once; three converter blocks pull the upper band, the lower band and the middle band out of the same indicator value.
- Band width is computed by a formula block and stored by a previous-value block, which turns expansion into a plain comparison of two numbers.
- Direction is not a breakout test: any expansion opens a trade, and the middle band only says whether it is a long or a short. That is exactly how the original strategy branches.
- As soon as the width stops growing, both close-position blocks fire and whichever side is open is flattened.

## Entry and Exit Rules

- **Long entry**: The width is wider than on the previous candle, the candle closed above the middle band and the position is flat. The order buys the shared volume at market.
- **Short entry**: The width is wider than on the previous candle, the candle closed at or below the middle band and the position is flat. The order sells the shared volume at market.
- **Exit**: The width is no longer growing, that is it is at or below the width of the previous candle. Both close-position blocks are triggered and the one matching the open side flattens it at market. The original strategy has no stop loss and no take profit, and neither does this diagram.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Bollinger Period | 20 | Averaging length of the Bollinger Bands, which sets how quickly the width reacts. |
| Bollinger Width | 2 | Standard deviation multiplier of the bands; a larger value widens both bands and the distance between them. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the Bollinger Bands indicator and, separately, a converter that reads the closing price.
- The formula block takes the upper band as a and the lower band as b and returns their difference as the band width.
- The width goes both into the previous-value block and straight into two comparisons, so expansion and its opposite are read off the same pair of numbers.
- Each logical AND joins expansion, the side of the middle band and the flat-position check; the exit blocks hang directly on the contraction comparison.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
