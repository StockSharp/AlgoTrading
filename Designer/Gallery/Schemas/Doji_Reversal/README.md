# Doji Reversal Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A doji is a candle that opens and closes at almost the same price: buyers and sellers spent the bar undoing each other. The diagram measures that indecision as a ratio of the body to the full range, and then lets the two closes before the doji decide which way to lean, because a doji on its own says nothing about direction. A simple moving average is the only way out.

![schema](schema.svg)

## Strategy Overview

- One formula block computes the body minus the range times the threshold, so a negative result means the body is smaller than the allowed fraction of the candle.
- Writing the test as a multiplication rather than a division also reproduces the guard of the original code: on a candle where high equals low the comparison is zero against zero and no doji is reported.
- Two previous-value blocks read the closing prices one and two candles back; a fall between them is treated as a downswing and is bought, a rise as an upswing and is sold.
- The original strategy also blocks every signal for several hundred bars after a fill; a bar counter has no block of its own, so that pause is left out and noted here.

## Entry and Exit Rules

- **Long entry**: The candle just finished is a doji, the close one candle back is lower than the close two candles back, and the position is zero. The order buys one lot and opens a long.
- **Short entry**: The candle just finished is a doji, the close one candle back is higher than the close two candles back, and the position is zero. The order sells one lot and opens a short.
- **Exit**: A long is closed by a position modify block in close mode once a candle closes below the moving average; a short is closed once a candle closes above it. The source strategy has neither a stop loss nor a take profit, and neither does this diagram.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Doji Threshold | 0.1 | Largest ratio of body to full range at which a candle still counts as a doji. |
| SMA Length | 20 | Averaging length of the simple moving average that closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on; the original runs on one-minute candles and is scaled here to the five-minute history shipped with the gallery. |

## Diagram Details

- The candle block feeds four converters for open, high, low and close, plus the moving average.
- The four prices and the threshold constant meet in a single formula block, and one comparison against zero turns its result into the doji flag.
- The closing price also goes into two previous-value blocks, whose outputs are compared against each other to give the direction of the last swing.
- Each logical AND joins the doji flag, one direction condition and the position guard, and fires an entry block; the two closing blocks are triggered straight from the comparisons against the moving average.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
