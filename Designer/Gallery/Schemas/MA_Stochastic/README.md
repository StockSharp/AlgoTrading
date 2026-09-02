# MA + Stochastic Pullback Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two blocks decide together: SimpleMovingAverage says which side of the market the diagram is allowed to take, and StochasticK waits for a move against that side before the order is sent. The trade is given back as soon as price closes on the other side of the same average.

![schema](schema.svg)

## Strategy Overview

- The close against SimpleMovingAverage sets the direction: above the average only longs are considered, below it only shorts.
- The entry itself is contrarian - the %K line of the Stochastic has to be in the oversold zone for a long and in the overbought zone for a short, so the diagram buys dips inside an uptrend and sells rallies inside a downtrend.
- StochasticK is exactly the %K the original strategy computed by hand: 100 * (Close - lowest Low) / (highest High - lowest Low) over the last N candles.
- The same moving average is also the exit line, and there is no stop loss or take profit anywhere in the diagram.

## Entry and Exit Rules

- **Long entry**: The close is above SimpleMovingAverage, StochasticK is below the oversold level, and the position is flat. The order buys one lot at market.
- **Short entry**: The close is below SimpleMovingAverage, StochasticK is above the overbought level, and the position is flat. The order sells one lot at market.
- **Exit**: A long is closed on the first candle that closes below the moving average, a short on the first candle that closes above it; the two closing blocks take their volume from the open position.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the SimpleMovingAverage that filters the trend and closes the position. |
| %K Length | 14 | Number of candles the %K line looks back over. |
| %K Oversold | 20 | Level below which %K is treated as oversold and a long is allowed. |
| %K Overbought | 80 | Level above which %K is treated as overbought and a short is allowed. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds three branches: the converter that reads the close, the SimpleMovingAverage and the StochasticK indicator.
- Two comparisons place the close against the average, two more place %K against the threshold constants, and one compares the position with zero.
- Each logical AND joins a trend condition, a Stochastic condition and the flat-position check, then triggers a position modify block that opens only from flat.
- The trend comparisons are reused by the exit: the same signal that allows a short also closes a long, which keeps the diagram small. The bar counter that paused the original strategy for 100 candles after every trade has no block of its own and is left out.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
