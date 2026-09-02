# Heikin-Ashi Reversal Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Heikin-Ashi candles average away most of the noise, so a run of them keeps one colour for as long as the move lasts and flips only when the balance really changes. This diagram trades that flip: the first bullish Heikin-Ashi candle after a bearish one buys, the first bearish one after a bullish one sells, and a simple moving average of the ordinary closing price decides when the trade is over.

![schema](schema.svg)

## Strategy Overview

- A formula block builds the Heikin-Ashi body as the average of open, high, low and close minus the midpoint of the previous candle; a positive body means a bullish Heikin-Ashi candle, zero or less a bearish one.
- A previous-value block keeps the body of the candle before, so the two comparisons together describe a colour change rather than just a colour.
- The moving average and the exit price are taken from the ordinary candles, not from the smoothed ones, exactly as in the source strategy.
- The Heikin-Ashi open is properly defined by its own previous value, which a diagram cannot feed back into a block; the midpoint of the previous raw candle is used instead, so the colour changes here are close to, but not identical with, the ones the original code computes.
- The original strategy also freezes every signal for several hundred bars after a fill; a bar counter has no block of its own, so that pause is left out and noted here.

## Entry and Exit Rules

- **Long entry**: The Heikin-Ashi body of the candle just finished is positive, the body of the candle before it was zero or negative, and the position is zero. The order buys one lot and opens a long.
- **Short entry**: The Heikin-Ashi body of the candle just finished is zero or negative, the body of the candle before it was positive, and the position is zero. The order sells one lot and opens a short.
- **Exit**: A long is closed by a position modify block in close mode once an ordinary candle closes below the moving average; a short is closed once one closes above it. The source strategy carries no stop loss and no take profit, and neither does this diagram.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average on the ordinary closing price, which closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on; the original runs on one-minute candles and is scaled here to the five-minute history shipped with the gallery. |

## Diagram Details

- The candle block feeds four converters for open, high, low and close, plus the moving average.
- Two previous-value blocks hand the formula the open and close of the candle before, which is what the Heikin-Ashi open is approximated with.
- A third previous-value block delays the formula result by one candle, and four comparisons against a zero constant turn the two bodies into the current and the previous colour.
- Each logical AND joins the new colour, the opposite old colour and the position guard, and fires an entry block; the two closing blocks are triggered straight from the comparisons against the moving average.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
