# VWMA and RSI Reversion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A volume weighted moving average marks where the money has actually traded, and RSI says whether the move away from it has been overdone. The diagram buys under the average only when RSI is in the oversold zone, sells above it only when RSI is overbought, and holds the trade until price crosses back to the other side of the average.

![schema](schema.svg)

## Strategy Overview

- The average is a rolling VolumeWeightedMovingAverage of 32 candles, not a session VWAP. It is the indicator the original strategy uses, despite the name, and it weights every close by the volume traded on that candle.
- The Relative Strength Index is calculated on close prices and only confirms an entry; on its own it opens nothing.
- Both indicator blocks emit formed values only, which is what keeps the diagram from trading on the incomplete average of the first candles.
- The original stops processing candles for 100 bars after each trade, which also freezes the exit and holds a position for at least eight hours. Designer has no lock-out counter, so that pause is not reproduced: here a position is closed as soon as price returns across the average.

## Entry and Exit Rules

- **Long entry**: The close is below the VWMA, RSI is under the oversold level and the position is flat. The order buys the configured volume.
- **Short entry**: The close is above the VWMA, RSI is over the overbought level and the position is flat. The order sells the configured volume.
- **Exit**: A long is closed once the close comes back above the VWMA, a short once the close comes back below it. There is no stop loss and no take profit, as in the original strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| VWMA Length | 32 | Number of candles in the volume weighted moving average. |
| RSI Length | 14 | Averaging length of the Relative Strength Index. |
| Oversold | 30 | Level below which the index is treated as oversold. |
| Overbought | 70 | Level above which the index is treated as overbought. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the volume weighted average directly, because that indicator needs the volume of the candle, and feeds RSI through a converter that reads the close price.
- Two comparison blocks put the close on one side of the average or the other, and the same two signals serve both the entries and the exits.
- Two more comparisons test RSI against the threshold constants.
- The position block is compared with zero three times, giving flat, long and short flags for the logical AND blocks.
- Each entry AND joins three conditions, price side, RSI extreme and a flat position, and triggers a position modify block with the Open position condition; the exits use blocks with the Close position condition, which need no volume.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
