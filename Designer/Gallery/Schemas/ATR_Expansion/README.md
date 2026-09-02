# ATR Expansion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Volatility itself is the signal here. The Average True Range is compared with its own value one candle earlier: when it jumps by at least the expansion ratio, something has started moving, and the diagram joins that move in the direction the simple moving average points. When the range shrinks back by the same ratio, the move is considered over and the position is closed.

![schema](schema.svg)

## Strategy Overview

- The Average True Range measures the size of the last candles; a previous-value block keeps the reading from one candle earlier so the two can be compared.
- Expansion is ATR at or above the previous ATR multiplied by the ratio; contraction is the mirror image, the previous ATR above ATR multiplied by the same ratio.
- The simple moving average only decides the side: the close above it makes the expansion a long, the close below it makes it a short.
- Both entry blocks carry the open-position condition and both exit blocks the close-position condition, so the diagram holds one position at a time and never adds to it.

## Entry and Exit Rules

- **Long entry**: Volatility is expanding, the candle closes above the simple moving average and the position is flat. The order buys the shared volume at market.
- **Short entry**: Volatility is expanding, the candle closes below the simple moving average and the position is flat. The order sells the shared volume at market.
- **Exit**: Volatility contracts, that is ATR multiplied by the ratio falls below the previous ATR. Whichever side is open is closed at market by the matching close-position block; there is no stop loss and no take profit, exactly as in the original strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| ATR Period | 14 | Averaging length of the Average True Range that measures volatility. |
| MA Period | 20 | Length of the simple moving average that decides the direction of the entry. |
| Expansion ratio | 1.05 | How much larger the new ATR must be than the previous one to count as expansion; its reciprocal is the contraction threshold that closes the position. |
| Volume | 1 | Order volume, in lots, shared by both entry blocks. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the ATR, the moving average and a converter that reads the close price.
- A previous-value block holds the ATR of the preceding candle, and two formula blocks multiply the ratio into it: one builds the expansion level, the other the contraction level.
- Two comparison blocks turn those levels into an expansion flag and a contraction flag, and two more place the close against the moving average.
- Each logical AND joins volatility, direction and a position-is-flat comparison, and triggers one of the two entry blocks; the contraction flag alone triggers the two close-position blocks, whose direction decides which side they may close.
- Two things from the C# original are not carried over: the five hundred bar pause after every trade, which has no equivalent block, and the one minute candles, replaced by the five minute candles the gallery history is shipped in.
- The unused Lookback parameter of the original is left out as well, because the code never reads it.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
