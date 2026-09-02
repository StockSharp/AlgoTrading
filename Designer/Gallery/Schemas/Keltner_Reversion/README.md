# Keltner Channel Reversion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A Keltner channel is a moving average with a volatility envelope around it: the width comes from the Average True Range, so the band breathes with the market instead of sitting at a fixed distance. This diagram treats a close outside the channel as an overshoot, takes the opposite side and gives the trade back at the middle line.

![schema](schema.svg)

## Strategy Overview

- The channel is assembled by hand rather than taken from the ready KeltnerChannels indicator, because that block ties the average and the ATR to one length, while the original uses 20 for the EMA and 14 for the ATR.
- Two formula blocks build the bands literally: EMA plus and minus ATR times the multiplier, with the multiplier exposed so the channel can be widened or narrowed without touching the diagram.
- The middle line is the whole exit rule: a trade is given back the moment price crosses back to the other side of the EMA, so the profit target moves with the average.
- The original works on one-minute candles and locks trading for 500 bars after every trade, which in practice also holds the position. The packaged history is five-minute data, so the diagram runs on five-minute candles; the lock-out is not reproduced, because the Designer has no bar counter that holds a state, and the diagram therefore trades more often and holds shorter.

## Entry and Exit Rules

- **Long entry**: The close is below the lower band, that is under the EMA by more than the ATR times the multiplier, and the position is flat. The order buys the configured volume.
- **Short entry**: The close is above the upper band, that is over the EMA by more than the ATR times the multiplier, and the position is flat. The order sells the configured volume.
- **Exit**: A long is closed once the close is back above the EMA, a short once the close is back below it. The original declares a stop loss multiplier but never uses it, so the diagram has no stop loss and no take profit either.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| EMA Length | 20 | Averaging length of the exponential moving average that forms the middle line. |
| ATR Length | 14 | Averaging length of the Average True Range that sets the channel width. |
| ATR multiplier | 2 | How many ATRs the bands sit away from the middle line. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the converter that reads the close price and both indicator blocks; the ATR needs the whole candle, so it is wired straight from the candle source.
- Each band is one formula block over three inputs: the EMA, the ATR and the shared multiplier constant.
- Four comparison blocks test the close against the two bands and against the middle line, and three more compare the position with zero.
- Each logical AND joins one price condition with one position condition; the entry blocks carry the Open position condition and a shared volume constant, while the closing blocks carry the Close position condition.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
