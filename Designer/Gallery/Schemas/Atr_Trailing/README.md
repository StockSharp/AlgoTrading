# ATR Trailing Stop Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entries are the simple part: from a flat position, a close above the moving average buys and a close below it sells. The interesting half is the exit, an ATR trailing stop — a line held a multiple of the Average True Range away from the price that follows a profitable move and never gives ground back, closing the position as soon as the close breaks through it.

![schema](schema.svg)

## Strategy Overview

- A twenty-period simple moving average splits the chart into an up side and a down side, and the direction of the close against it decides which way to enter.
- The trailing stop is a SuperTrend block: it is exactly an ATR band with a ratchet, so the stop distance breathes with volatility instead of being a fixed number of points.
- Every entry is taken only from a flat position, and every exit only from a position of the matching side, which is what keeps the four order blocks from interfering with each other.
- The stop level is wide by design — three times a fourteen-period ATR — so a position is meant to survive normal noise and be given up only when the move genuinely turns.

## Entry and Exit Rules

- **Long entry**: The position is flat and the candle closes above the simple moving average. The order buys the shared volume at market and the ATR trailing line below the price becomes the stop for that long.
- **Short entry**: The position is flat and the candle closes below the simple moving average. The order sells the shared volume at market and the ATR trailing line above the price becomes the stop for that short.
- **Exit**: A long is closed when the close falls below the ATR trailing line, a short when the close rises above it. There is no take-profit and no reversal: after the stop the diagram waits flat for the next moving average signal.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| MA Period | 20 | Length of the simple moving average that decides the direction of the entry. |
| ATR Period | 14 | ATR length inside the trailing line; longer values make the stop react more slowly to a change in volatility. |
| ATR Multiplier | 3 | How many ATRs the trailing line is held away from the price; larger values give the position more room and fewer exits. |
| Volume | 1 | Order volume, in lots, shared by all four order blocks. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the moving average, the SuperTrend trailing line and a converter that reads the close price.
- Two comparisons place the close against the moving average and two more place it against the trailing line, so the same price is read once and used by both halves of the diagram.
- Three comparisons against a zero constant turn the position into flat, long and short flags that gate the entries and the exits separately.
- The two entry blocks carry the open-position condition and the two exit blocks the close-position condition, so a signal that does not fit the current position simply does nothing.
- The original strategy recomputes its stop level as the running maximum of close minus a multiple of ATR; that ratchet is not expressible as a chain of blocks, so the SuperTrend line, which ratchets the same way, stands in for it.
- Two further simplifications are worth knowing: the five hundred bar pause the original keeps after every trade has no equivalent block and is dropped, and the diagram runs on five minute candles rather than the one minute candles of the C# code, because that is the history the gallery ships.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
