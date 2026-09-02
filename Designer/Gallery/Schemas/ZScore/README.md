# Z-Score Mean Reversion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The close is turned into a z-score, the distance from a moving average measured in standard deviations, so that one number describes how stretched the market is no matter what the instrument costs. The diagram fades a stretched market and gives the trade back as soon as the score returns close to zero.

![schema](schema.svg)

## Strategy Overview

- The z-score is assembled by hand from SimpleMovingAverage and StandardDeviation: (Close - SMA) / StandardDeviation, computed in a single formula block.
- A mirrored formula produces the negative of the same score, so one exposed entry level and one exposed exit level cover both directions instead of four separate constants.
- Entries are made from a flat position only; the entry blocks additionally carry the Open position condition, so the diagram never averages into a trade it already holds.
- The original runs on one-minute candles and locks trading for 500 bars after every trade. The packaged history is five-minute data, so the diagram works on five-minute candles, and the lock-out is not reproduced because the Designer has no bar counter that holds a state; the diagram therefore trades more often and holds shorter than the original.

## Entry and Exit Rules

- **Long entry**: The z-score is below minus the entry level, that is the close sits more than the configured number of standard deviations under the average, and the position is flat. The order buys the configured volume.
- **Short entry**: The z-score is above plus the entry level, that is the close sits more than the configured number of standard deviations above the average, and the position is flat. The order sells the configured volume.
- **Exit**: A long is closed once the z-score climbs back above the exit level, a short once the z-score falls back below minus the exit level. There is no stop loss and no take profit, exactly as in the original strategy.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| SMA Length | 10 | Averaging length of the moving average that the score is measured from. |
| StandardDeviation Length | 10 | Length of the standard deviation that the distance is divided by. |
| Entry z-score | 1.5 | Distance from the average, in standard deviations, that opens a trade. |
| Exit z-score | 0.5 | Distance from the average, in standard deviations, at which an open trade is given back. |
| Volume | 1 | Order volume, in lots. |
| Candles | 00:05:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the converter that reads the close price and both indicator blocks, which are set to send values only once they are formed.
- Two formula blocks build the score and its negative from the same three inputs, so the mirrored comparisons need no extra constants.
- Four comparison blocks test the two scores against the entry and exit levels, and three more compare the position with zero.
- Each logical AND joins one score condition with one position condition; the two entry blocks take their volume from a shared constant, while the two closing blocks use the Close position condition and need none.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
