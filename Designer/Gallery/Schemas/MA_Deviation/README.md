# Moving Average Deviation Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A simple moving average is treated as fair value, and the distance of the close from it, measured in percent, is the whole signal. When price has run too far from the average the diagram takes the opposite side, and it gives the trade back as soon as price touches the average again.

![schema](schema.svg)

## Strategy Overview

- The deviation is computed literally, in one formula block: (Close - SMA) / SMA * 100.
- One threshold serves both sides: the diagram compares the deviation with the plus and the minus of the same number, so long and short are symmetric.
- Entries are made from a flat position only, and both entry blocks additionally carry the Open position condition, so the diagram never averages down.
- The original works on one-minute candles with a 2% threshold and a 500-bar cooldown after every trade. The packaged history is five-minute data, so the diagram runs on five-minute candles with a 1% threshold, which is roughly two standard deviations of that series; the cooldown is not reproduced, because the Designer has no lock-out counter, and the diagram therefore trades more often than the original.

## Entry and Exit Rules

- **Long entry**: The deviation is below minus the threshold, that is the close is more than the configured percentage under the moving average, and the position is flat. The order buys the configured volume.
- **Short entry**: The deviation is above plus the threshold, that is the close is more than the configured percentage above the moving average, and the position is flat. The order sells the configured volume.
- **Exit**: A long is closed once the close returns to the moving average or above it; a short is closed once the close returns to the average or below it. There is no stop loss and no take profit, as in the original strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 20 | Averaging length of the simple moving average. |
| Deviation, % | 1 | Distance from the average, in percent, that opens a trade. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds both the converter that reads the close price and the indicator block holding the moving average.
- A formula block turns the pair into a percentage deviation; a second, tiny formula flips the threshold constant to its negative so that one exposed number covers both sides.
- Two comparison blocks test the deviation against the thresholds, and two more compare the close with the average for the exits.
- The position block is compared with zero three times, giving flat, long and short flags that the logical AND blocks join with the price conditions.
- Entries go to position modify blocks with the Open position condition and a shared volume constant; exits go to blocks with the Close position condition, which need no volume.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
