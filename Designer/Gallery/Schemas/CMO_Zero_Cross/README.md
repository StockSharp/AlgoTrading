# CMO Zero Cross Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The Chande Momentum Oscillator swings between -100 and +100 and changes sign exactly when buying pressure and selling pressure trade places. This diagram trades that sign change, but only when the new reading is already far enough from zero to be worth an order, so the flat drift around the zero line is ignored.

![schema](schema.svg)

## Strategy Overview

- The Chande Momentum Oscillator is calculated on finished hourly candles of a single instrument.
- The cross is read from two values, the oscillator one candle back and the oscillator now, instead of a crossing block, which makes the direction of the move explicit on the picture.
- A strength filter demands that the new reading is at least the minimum distance away from zero, which throws away the shallow crosses that happen while the market is going nowhere.
- The position takes part in every decision and also sets the order size, so a signal against an open trade reverses it with a single market order.

## Entry and Exit Rules

- **Long entry**: The oscillator was below zero on the previous candle and is now at or above the minimum positive level, and the position is not long. The order buys the shared volume plus the size of an open short, so one market order closes the short and opens the long.
- **Short entry**: The oscillator was at or above zero on the previous candle and is now at or below the minimum negative level, and the position is not short. The order sells the shared volume plus the size of an open long.
- **Exit**: There is no separate exit block: a position is left either by the opposite zero cross, which reverses it, or by the position protection block. The original uses an absolute take profit of 2000 and stop loss of 1000 price steps; absolute levels tuned for another instrument would never be reached on this history, so they are written here as a two percent target and a one percent stop, which keeps the same two-to-one ratio. The original also pauses for four candles after every position change; there is no block that keeps a bar counter between candles, so the pause is dropped and the position guard alone prevents a second entry in the same direction.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| CMO Length | 14 | Averaging length of the Chande Momentum Oscillator. |
| Min |CMO| | 5 | Minimum distance from zero the oscillator must reach for the cross to count. |
| Volume | 1 | Order volume, in lots. |
| Take profit, % | 2 | Take profit distance from the entry price, in percent. |
| Stop loss, % | 1 | Stop loss distance from the entry price, in percent. |
| Candles | 01:00:00 | Candle time frame the whole diagram works on. |

## Diagram Details

- The candle block feeds the indicator block with the Chande Momentum Oscillator and a converter that picks the closing price for the protection block.
- A previous value block holds the oscillator one candle back, and two comparison blocks decide which side of zero it was on.
- The strength constant is fed straight into the long comparison and, through a small formula that negates it, into the short comparison, so one parameter controls both sides.
- Each logical AND joins the previous side, the strength filter and the position guard, and triggers a position modify block whose volume comes from the formula that adds the absolute position to the shared volume.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
