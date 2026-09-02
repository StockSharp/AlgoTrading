# ATR Range Breakout Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

One number decides everything here: how far the close has travelled over the last few candles, measured against the Average True Range. A move at least one ATR wide is treated as a breakout worth joining, and the side is simply the side the price moved. The simple moving average is not used to enter at all - it is the exit, and the position is given up as soon as the close falls back through it.

![schema](schema.svg)

## Strategy Overview

- A previous-value block holds the close four candles back, and a formula block subtracts it from the current close and takes the absolute value: that is the distance travelled.
- The Average True Range is the yardstick. When the distance reaches it, the market has moved more in those four candles than it usually moves in one, and the diagram calls that a breakout.
- Direction needs no indicator: the close above the earlier close means a long, the close below it a short.
- The moving average has one job only, closing the position: a long ends on the first close below it, a short on the first close above it.

## Entry and Exit Rules

- **Long entry**: The distance travelled over the last four candles is at least one ATR, the close is above the close four candles ago and the position is flat. The order buys the shared volume at market.
- **Short entry**: The distance travelled over the last four candles is at least one ATR, the close is below the close four candles ago and the position is flat. The order sells the shared volume at market.
- **Exit**: A long is closed on the first candle that closes below the simple moving average, a short on the first candle that closes above it. Both exit blocks carry the close-position condition, so each of them can only act on the side it is meant for. There is no stop loss and no take profit, as in the original strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| ATR Period | 14 | Averaging length of the Average True Range that sets the minimum width of a breakout. |
| MA Period | 20 | Length of the simple moving average that closes the position. |
| Lookback shift | 4 | How many candles back the price is compared with; the original measures over the lookback window minus one, which is four candles by default. |
| Volume | 1 | Order volume, in lots, shared by both entry blocks. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the ATR, the moving average and a converter that reads the close price; the previous-value block hangs off that converter.
- The formula block computes the absolute difference between the two closes, and a comparison holds it against the ATR to decide whether the move is wide enough.
- Two comparisons of the same pair of closes give the direction, and one comparison of the position against a zero constant keeps the entries from stacking.
- Each logical AND joins range, direction and the flat position, and triggers one open-position block; the two moving average comparisons trigger the close-position blocks directly, since the direction of a close-position block already decides which side it may close.
- The C# original only measures every fifth candle, over windows that do not overlap, and freezes the reference price on the bar in between. That modulo counter has no equivalent block, so the diagram uses a sliding window instead and checks on every candle, which produces more signals than the original.
- The five hundred bar pause the original keeps after every trade is dropped for the same reason, and the diagram runs on the five minute candles the gallery history is shipped in rather than the one minute candles of the C# code.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
