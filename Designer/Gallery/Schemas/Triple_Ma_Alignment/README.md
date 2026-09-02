# Triple EMA Alignment Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Three ExponentialMovingAverage blocks of very different lengths are stacked on the same candles, and the diagram treats their order as the trend. When the short average is above the middle one and the middle one above the long one, the market is up; when the three line up the other way, it is down. The strategy is always in the market and flips sides with one order.

![schema](schema.svg)

## Strategy Overview

- Only price is used: no oscillator, no volatility filter, just the relative order of three exponential averages.
- The bullish state is short above middle and middle above long; the bearish state is short at or below middle and middle at or below long. In between, when the averages are tangled, nothing happens.
- The current position gates every entry, so an alignment that lasts for hundreds of candles still produces exactly one order.
- There is no separate exit: the order size is the volume plus the absolute position, so a single order both closes the old side and opens the new one.

## Entry and Exit Rules

- **Long entry**: The short ExponentialMovingAverage is above the middle one, the middle one is above the long one, and the position is not already long. The order buys the volume plus the absolute position, which opens a long from flat or reverses a short into a long.
- **Short entry**: The short ExponentialMovingAverage is at or below the middle one, the middle one is at or below the long one, and the position is not already short. The order sells the volume plus the absolute position, which opens a short from flat or reverses a long into a short.
- **Exit**: There is no exit block of its own. A position is left only when the opposite alignment appears, and the reversing order size means the diagram is flat for no candle at all.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Short EMA period | 100 | Length of the fastest ExponentialMovingAverage. |
| Middle EMA period | 250 | Length of the middle ExponentialMovingAverage. |
| Long EMA period | 500 | Length of the slowest ExponentialMovingAverage. |
| Volume | 1 | Base order volume, in lots; the absolute position is added on top when reversing. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- One candle block feeds all three indicator blocks, so the averages are always computed on the same finished candles.
- Four comparison blocks build the two states: two strict greater-than for the bullish stack, two less-or-equal for the bearish one, which is exactly the negation used in the original code.
- Each logical AND joins the two average comparisons with the position compared against a zero constant and triggers one position-modify block.
- A formula block adds the absolute position to the volume constant and feeds both order blocks, which is what turns an entry into a reversal.
- Deliberate simplifications: the original runs on one-minute candles, and this diagram runs on five-minute ones, so the same lengths cover five times as much time. The original also remembers whether the alignment was already there on the previous candle; that flag is dropped, because the position guard blocks a repeat entry just as effectively. The declared 2% stop loss is never applied in the original code, so no protection block is drawn.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
