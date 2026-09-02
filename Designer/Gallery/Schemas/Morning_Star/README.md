# Morning Star Reversal Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The Morning Star is the classic three-candle bottom: a wide down candle, a small hesitant candle, and a wide up candle that recovers more than half of the first one. Its mirror image, the Evening Star, marks a top. This diagram recognizes both shapes with candle pattern blocks, opens a position only when it is flat, and hands the trade back as soon as price closes on the wrong side of a simple moving average.

![schema](schema.svg)

## Strategy Overview

- Two candle pattern indicator blocks carry custom three-candle expressions: the first candle has a real body and points the wrong way, the middle body is smaller than half of it, and the third candle closes past the midpoint of the first.
- A simple moving average of the closing price is the only exit reference; the diagram has no stop loss and no take profit, exactly as in the original strategy.
- The position block is compared against zero so a pattern is acted on only from a flat book, never as an addition to an open trade.
- The original strategy also freezes every signal for several hundred bars after each fill; a bar counter has no block of its own, so that pause is left out and noted here.

## Entry and Exit Rules

- **Long entry**: The Morning Star block reports the pattern on the candle just finished and the position is zero. The order buys one lot and opens a long.
- **Short entry**: The Evening Star block reports the pattern on the candle just finished and the position is zero. The order sells one lot and opens a short.
- **Exit**: A long is closed by a position modify block in close mode as soon as a candle closes below the moving average; a short is closed the same way once a candle closes above it. There is no protective stop, because the source strategy has none either.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average that closes the trades. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on; the original runs on one-minute candles and is scaled here to the five-minute history shipped with the gallery. |

## Diagram Details

- The candle block feeds four branches: the two pattern indicators, the moving average and a converter that reads the closing price.
- Each pattern block holds a three-condition expression pattern, so the shape is recognized without a chain of formula blocks.
- Two comparison blocks put the closing price on one side of the moving average or the other and trigger the two closing blocks directly.
- Each logical AND joins one pattern with the position guard and fires an entry block; both entry orders take their volume from one shared constant, while the closing blocks compute it from the open position.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
