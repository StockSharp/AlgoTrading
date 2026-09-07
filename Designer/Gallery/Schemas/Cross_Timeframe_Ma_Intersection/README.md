# Cross Timeframe MA Intersection Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This diagram synchronizes an hourly candle with the completed values of a fast and a slow exponential moving average before evaluating their crossover. It opens one unit on the first signal and uses twice the base volume on every opposite signal, so each trade reverses the position without increasing its size.

![schema](schema.svg)

## Strategy Overview

- One finished hourly candle series feeds both EMA(20) and EMA(50), keeping their price basis identical.
- The Sync block forms one hourly group containing the candle, fast EMA and slow EMA; no crossover decision is made from an incomplete group.
- A Crossing block emits `true` when the fast EMA moves above the slow EMA and `false` when it moves below; a NOT block turns the latter into the short trigger.
- Position-sign checks select either a base-volume opening action from flat or a two-times-base reversal action from the opposite side.
- The chart receives the synchronized candle and both synchronized EMA values, together with all strategy fills.

## Entry and Exit Rules

- **Long entry**: When synchronized EMA(20) crosses above EMA(50), buy one base volume from flat or two base volumes while short, leaving a one-base-volume long position.
- **Short entry**: When synchronized EMA(20) crosses below EMA(50), sell one base volume from flat or two base volumes while long, leaving a one-base-volume short position.
- **Exit**: There is no separate stop, target or timed exit. The next opposite crossover supplies a market reversal whose volume closes the current unit and opens one unit in the new direction.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Fast EMA Length | 20 | Length of the faster exponential moving average. |
| Slow EMA Length | 50 | Length of the slower exponential moving average. |
| Candles | 01:00:00 | Time frame of the finished candles used by both averages. |
| Sync interval | 01:00:00 | Time range used by Sync to group the candle and both indicator values. |
| Base volume | 1 | Position size opened from flat; a reversal automatically uses twice this value. |

## Diagram Details

- The candle output first updates the position snapshot, zero and volume constants, then feeds EMA(20) and EMA(50); its final connection is the third Sync input and therefore completes the hourly group after both calculations.
- Sync Input 1 receives EMA(20), Input 2 receives EMA(50), and Input 3 receives the candle. All three paired outputs are connected and the block clears each completed group.
- Sync Output 1 and Output 2 feed Crossing Input Up and Input Down. Output 3 passes through a ClosePrice converter and also supplies the chart candle series.
- Four logical routes distinguish flat, long and short position states for the two crossover directions. A same-side signal cannot add to an existing position.
- Four Modify position blocks send market orders: two opening blocks use the base volume, while two reversal blocks use the formula `2 × base volume`.
- Because every open position starts with the base volume, the reversal amount equals `abs(position) + base volume` and leaves the exposure unchanged in magnitude.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
