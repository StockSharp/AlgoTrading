# Two Symbol Spread Deviation Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two instruments that usually move together occasionally do not, and the gap between them tends to close again. The diagram divides one price by the other, watches how far that ratio strays from its own moving average, and buys or sells the first instrument at the moment the gap starts closing rather than at the moment it opens.

![schema](schema.svg)

## Strategy Overview

- An index block builds one synthetic security out of two real ones by dividing the price of the first by the price of the second, and a candle series is subscribed to that synthetic security, so the spread arrives as ready-made candles instead of being assembled by hand.
- A moving average runs over the spread candles and a converter takes their close price, which gives the spread both a current level and a reference line of its own.
- A formula reduces the pair to a single number: the distance from the close to the average, in percent of the average.
- The same reading one bar back is rebuilt from a previous spread candle and a previous average value, and a second formula turns those two into the deviation of the previous bar.
- A second candle series runs on the traded instrument, and two variables latch the two deviations onto it: each holds the last number the spread produced and releases it when a traded candle finishes, so every decision is taken on the clock of the instrument the orders go to.
- Comparisons then ask four questions: where the previous deviation stood against the threshold, which way the deviation is moving now, which side of the average it is still on, and whether the position is flat. A logical condition collects the four answers into one entry gate per direction.
- Entries are market orders of a fixed size taken only from a flat position, and they are sent for the instrument the strategy itself is set to. The synthetic security is a source of numbers and never carries an order.
- Two further comparisons watch the deviation reach the average again and hand that moment to two position blocks, one for each side, which close whatever is open.

## Entry and Exit Rules

- **Long entry**: The deviation of the previous bar was below the lower threshold, this bar's deviation is higher than the previous one, the deviation is still under the average, and the position is flat. The long block buys the order volume at market.
- **Short entry**: The deviation of the previous bar was above the upper threshold, this bar's deviation is lower than the previous one, the deviation is still over the average, and the position is flat. The short block sells the order volume at market.
- **Exit**: A position is closed when the spread finishes the journey it was bought for: a long goes when the deviation reaches the average or crosses above it, a short when it reaches the average or crosses below. Each closing block carries a side of its own, so the one meant for longs cannot touch a short and a closing block fired on a flat position simply does nothing. There is no profit target, stop-loss or time limit here; the return of the spread is the whole exit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | The expression the synthetic security is built from: the price of the first instrument divided by the price of the second. Both instruments have to exist in the connected data, and only the first of them is traded. |
| Spread Candles | 00:05:00 | Length of the candles the spread series is built on. |
| Traded Candles | 00:05:00 | Length of the candles the orders are placed on. Keep it equal to the spread series, or the latched numbers will be older than the bar they are read on. |
| Average Length | 20 | Number of spread candles in the moving average the deviation is measured from. |
| Deviation Threshold, % | 0.3 | How far the spread has to travel from its average, in percent, before a return towards it is worth trading. |
| Order Volume | 1 | Order size, in lots. |

## Diagram Details

- The deviation of the previous bar is rebuilt from a previous candle and a previous average value rather than by holding the finished percentage, so both halves of the ratio are read one bar back and the comparison of now against then cannot mix two different moments.
- The two series do not finish at the same instant: a synthetic security is assembled from two feeds and its candle is closed a little after the plain one. Latching the numbers on the traded candle is what keeps the strategy on a single clock; releasing both series together instead would date every order by the earlier of the two, and an order dated before the current time is refused.
- The constants are triggered by the traded candle. A comparison needs both of its values to arrive again for every evaluation, so a constant that is not re-sent stops the condition it feeds without any sign of it.
- The lower threshold is not a second constant but a formula over the same value, so the band stays symmetrical whatever the threshold is set to.
- The order blocks are told not to wait for the strategy to come online, and the entries are set to open only from a flat position, so a signal that repeats while a trade is running adds nothing to it.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
