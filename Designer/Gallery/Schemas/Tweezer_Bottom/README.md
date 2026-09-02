# Tweezer Bottom Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A tweezer is a pair of neighbouring candles that turn against each other on the same price level: after a down candle an up candle stops at almost the same low, and the pair marks a floor. The mirror image on the highs marks a ceiling. Because two lows almost never match to the last tick, the diagram measures the distance between them as a percentage and calls it a match while that distance stays under the tolerance.

![schema](schema.svg)

## Strategy Overview

- A candle pattern block recognises only the colour change of the pair: a down candle followed by an up candle for the bottom, an up candle followed by a down candle for the top.
- The equality of the extremes is measured separately by a formula, so the tolerance stays a schema parameter that can be optimized instead of being frozen inside the pattern text.
- The simple moving average takes no part in the entry; it only decides when the trade is over.
- Every entry is guarded by the position, so a tweezer is a reversal attempt and never an addition to a running trade.

## Entry and Exit Rules

- **Long entry**: The pattern block reports a down candle followed by an up candle, the distance between the two lows is at most the tolerance percent of the previous low, and the position is flat. The order buys the shared volume at market.
- **Short entry**: The pattern block reports an up candle followed by a down candle, the distance between the two highs is at most the tolerance percent of the previous high, and the position is flat. The order sells the shared volume at market.
- **Exit**: A long is closed by the first candle that closes below the simple moving average, a short by the first candle that closes above it; both exits are position modify blocks in close mode, so they never open anything. The original has no stop loss and no take profit, and neither does this diagram. Two things from the original could not be expressed with the blocks available: the pause of five hundred bars after every trade, because no block keeps a bar counter between candles, and the exact one minute time frame, which was scaled to the five minute candles of the packaged history.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Tolerance, % | 0.1 | How far the two extremes may sit apart, as a percent of the previous candle's level. |
| SMA Length | 20 | Averaging length of the simple moving average that closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both pattern blocks, the moving average and three converters that read the low, the high and the close.
- Two previous-value blocks hold the low and the high of the candle before, and two formulas turn each pair into the distance between the extremes in percent.
- Two comparisons test those distances against the shared tolerance constant, and one more comparison tests the position against zero.
- Each logical AND joins the pattern, the matching extremes and the flat check, then triggers an entry block that takes its volume from the shared constant.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
