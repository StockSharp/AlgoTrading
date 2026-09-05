# Outside Bar Reversal Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

An outside bar is a candle that swallows the whole range of the one before it: a higher high and a lower low in the same bar. Both sides were given their chance inside a single candle and one of them won, so the diagram reads the winner off the body of the bar itself — a bullish outside bar is bought, a bearish one is sold. A simple moving average of the closing price then decides when to let the trade go.

![schema](schema.svg)

## Strategy Overview

- The outside bar is assembled from plain blocks: converters read the high, the low, the open and the close of the finished candle, and two previous-value blocks hold the high and the low of the candle before it.
- Two comparisons make the shape — the high above the previous high and the low below the previous low — and both must hold at once.
- Direction comes from the candle's own body, not from a trend filter: closing above the open means buy, closing below it means sell.
- The simple moving average takes no part in the entry and serves only as the exit line.

## Entry and Exit Rules

- **Long entry**: The candle has taken out both extremes of the previous one, it closed above its own open and the position is flat. The order buys one lot and opens a long.
- **Short entry**: The candle has taken out both extremes of the previous one, it closed below its own open and the position is flat. The order sells one lot and opens a short.
- **Exit**: A long is closed once a candle closes below the moving average, a short once a candle closes above it, both through position modify blocks in close mode. There is no stop loss and no take profit. The diagram acts on every outside bar while the corresponding position guard allows it.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Five-minute candle time frame used to match the packaged history. |

## Diagram Details

- The candle block feeds five branches: four converters for the open, high, low and close, plus the moving average.
- The high and the low each go two ways at once — straight into a comparison and into a previous-value block — so the comparison holds this candle's extreme against the extreme of the candle before it.
- Each logical AND gathers four flags: the higher high, the lower low, the direction of the body and the position guard built from the position block against a zero constant.
- Both entry blocks send market orders and take their volume from one shared constant; the two exit blocks are triggered straight from the moving average comparisons and act only when there is something to close.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
