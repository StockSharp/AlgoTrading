# Scalping EMA Cross with RSI and MACD Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A short-term trend follower that refuses to take a cross on trust. The fast EMA crossing the slow one is only the trigger; before an order is sent the price must also sit on the right side of a much slower trend EMA, the RSI must be inside its working band rather than at an extreme, and the MACD line must still be moving in the direction of the trade. Every position is handed to a protective stop and target, so a scalp is never left open indefinitely.

![schema](schema.svg)

## Strategy Overview

- Three exponential moving averages do different jobs: the fast and slow pair produce the signal, the long one says which side of the market is allowed at all.
- The crossing block fires only at the moment the fast average changes sides, so a single trend never produces a stream of entries.
- RSI is used as a filter of extremes rather than as a signal: a cross is accepted only while the index stays between the floor and the ceiling, which keeps the diagram out of exhausted moves.
- The MACD line is compared with its own value one candle back, so momentum has to agree with the cross rather than merely exist.
- The position guard means an entry can only ever open a trade, never enlarge one.

## Entry and Exit Rules

- **Long entry**: The fast EMA crosses above the slow EMA, the candle closes above the trend EMA, RSI is between the floor and the ceiling, the MACD line is higher than one candle ago and the position is flat. The order buys the shared volume at market.
- **Short entry**: The fast EMA crosses below the slow EMA, the candle closes below the trend EMA, RSI is between the floor and the ceiling, the MACD line is lower than one candle ago and the position is flat. The order sells the shared volume at market.
- **Exit**: The position protection block closes every trade on a percentage stop or target measured from the fill price. The original sizes both levels off the Average True Range, stop at two ATR and target at twice that risk, but the protection block only accepts a fixed value, so the ATR distance was replaced by a percentage of the same order of magnitude on this instrument; changing it back means recomputing the levels in the diagram and sending the orders by hand. Two further things were left out: the pause of ten bars after every trade, which no block can count between candles, and the reversal on the opposite signal, since here the stop and the target end the trade instead. The original works on thirty minute candles and this diagram runs on the five minute candles of the packaged history.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast EMA Length | 12 | Length of the fast exponential moving average that produces the cross. |
| Slow EMA Length | 26 | Length of the slow exponential moving average the fast one is crossed against. |
| Trend EMA Length | 55 | Length of the trend exponential moving average that decides which side is allowed. |
| RSI Length | 14 | Averaging length of the Relative Strength Index. |
| RSI floor | 35 | Lower edge of the RSI band; below it a cross is treated as a move that has already run. |
| RSI ceiling | 65 | Upper edge of the RSI band; above it a cross is treated as overheated. |
| Take profit, % | 1 | Take profit distance from the fill price, in percent. |
| Stop loss, % | 0.5 | Stop loss distance from the fill price, in percent. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds all five indicators and a converter that reads the closing price; the MACD is built on the same twelve and twenty six lengths as the EMA pair.
- The crossing block takes the fast average on its up input and the slow one on its down input, and a logical NOT turns the same output into the downward cross for the short side.
- The RSI band is two comparisons against two constants, and both entries share them; the MACD momentum test compares the line with a previous-value block one candle back.
- Each logical AND gathers the cross, the trend side, both RSI edges, the momentum test and the flat check, then triggers an entry block that takes its volume from the shared constant.
- Both entry blocks send their own trades into the position protection block, which is what closes the position.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
