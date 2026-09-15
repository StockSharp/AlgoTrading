# Global Stop Timer Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entries are decided by price, exits are decided by money. Momentum crossing its neutral level opens a position in the direction the moving average already points, and from that moment the open result of the strategy owns the trade: the P&L block watches every change of the unrealized figure and flattens the position as soon as it falls to the money stop or reaches the money target, whatever the indicators happen to be saying.

![schema](schema.svg)

## Strategy Overview

- One candle series, five minutes and finished bars only, feeds everything: both indicators, the close-price converter and the triggers of the constant variables.
- Momentum measures how far the price has travelled over its own length; the Crossing block compares it with a level held in a variable and fires only at the moment the two swap sides, true for an upward cross and false for a downward one.
- A logical NOT turns the same crossing into the downward event, so one Crossing block serves both directions and the two can never fire on the same bar.
- A converter takes the close price out of the candle and two comparisons place it above or below the moving average, which is the trend filter both entries have to pass.
- The Position block is compared with zero three times: flat guards the entries, long and short arm the two signal exits.
- Each entry gate is a logical AND of three answers - the crossing, the side of the moving average and a flat position - and the entry itself is a market order taken only from flat, so a signal that arrives while a trade is running changes nothing.
- The P&L block has no inputs and no candle beat of its own: it emits the unrealized result on every change, and two comparisons measure that number against the money stop and the money target held in variables.
- The money verdict and the two opposite-cross verdicts arrive at one combination block, which drives a single Position modify set to close: whichever reason comes first, the position is flattened by the same market order.

## Entry and Exit Rules

- **Long entry**: Momentum crosses its level upward, the candle closes above the moving average and the position is flat. Position modify buys the order volume at market.
- **Short entry**: Momentum crosses its level downward - the same crossing block read through the logical NOT - the candle closes below the moving average and the position is flat. Position modify sells the order volume at market.
- **Exit**: Three reasons close a trade and all three end at the same block. The unrealized result of the strategy falls to the money stop; or it reaches the money target; or momentum crosses back through its level while the position is open in the opposite direction. The first two are checked on every P&L change rather than on the candle beat, so a fast move is answered inside the bar; the third is checked once per finished candle. The closing order is sized from the current position, so it always leaves the strategy flat and never reverses in one step.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series the whole diagram runs on; only finished candles are used. |
| Momentum Length | 10 | Length of the momentum indicator - how far back the price move is measured. |
| EMA Length | 20 | Length of the moving average that decides which side of the trend an entry is allowed on. |
| Momentum Level | 0 | Level the momentum is compared with. Zero is its neutral value: above it the price is higher than it was a length ago, below it lower. |
| Order Volume | 1 | Order size, in lots, for both entries. The closing order ignores it and is sized from the position. |
| Money Stop | -250 | Loss on the open position, in the currency of the account, at which the position is closed. Negative. |
| Money Target | 500 | Profit on the open position, in the currency of the account, at which the position is closed. |

## Diagram Details

- Both indicators are set to emit formed and final values only, so an unfinished candle cannot produce a crossing.
- The level the momentum is measured against lives in a variable rather than inside the comparison, which is what makes it a schema parameter that can be changed and optimized without opening the diagram.
- Every constant variable is triggered - the three candle-driven ones by the candle series, the two money thresholds by the unrealized P&L - because a variable holds its value but emits it only when something asks.
- The entries carry the open-position condition, so they fire only from flat. Without it a market order would be sent on every change of the position and the run would fill with unwanted trades.
- The money stop and the money target are amounts in the currency of the account, not distances in price, so they have to be rescaled together with the order volume when the diagram is moved to another instrument.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
