# EMA Cross with a Trailing Stop Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

The entry in this diagram is the plainest thing in the palette: a closing price crossing an exponential average on four-hour candles. What the example is actually about is the block that takes the position away afterwards. Position protection is armed by the entry fill, is handed a price on every closed candle and, with trailing switched on, drags the stop along behind a trade that is going its way, so the exit is a level that ratchets instead of a line fixed at entry.

![schema](schema.svg)

## Strategy Overview

- Four-hour candles are the clock of the diagram, and only finished ones are published, so every decision is taken on a bar that is already complete.
- A converter reads the closing price out of each candle, and an exponential average built on the same series is the level that price is measured against.
- Two crossing blocks watch that pair from opposite sides: one takes the close as its upper input and the average as its lower one, the other has them the other way round. Each speaks only on the bar where the two lines actually swap places.
- The current position is snapped onto the candle beat by a variable that holds it on its input and releases it on the candle trigger; three comparisons against zero turn that held number into flat, long and short.
- Four logical AND gates pair the two crossings with those three states: a crossing up while flat opens a long, a crossing down while flat opens a short, and either crossing against an existing position flattens it.
- Both entry blocks carry the open-position condition, so a gate that fires while a trade is already running can neither add to it nor reverse it: the diagram holds one position at a time.
- Every own fill reaches Position protection through a Combination, which is what keeps its idea of the position honest — entries arm it, flattening fills disarm it.
- Its Price socket is fed the same closing price the signals use, so the trailing level is recalculated once per closed candle; the chart panel draws the candles, the average, the entry and exit orders, the protective stop and every fill.

## Entry and Exit Rules

- **Long entry**: On a closed candle the closing price crosses above the exponential average while the held position is zero. The long gate passes and Position modify buys the order volume at market.
- **Short entry**: On a closed candle the closing price crosses below the exponential average while the held position is zero. The short gate passes and Position modify sells the order volume at market.
- **Exit**: Two things can end a trade, and usually the first one does. Position protection places its stop 1.5% away from the entry price and, with trailing on, carries it up behind a long and down behind a short every time a candle closes further into profit; the trade is closed by a market order as soon as a close comes back through that level. The second way out is the signal itself: a crossing against an open position triggers a third Position modify block set to close, which flattens whatever is there and needs no volume of its own. Neither path reverses a position — the opposite direction has to wait for the next crossing, by which time the diagram is flat and free to take it.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 04:00:00 | Time frame of the candle series. One closed candle is one decision and one recalculation of the trailing level. |
| EMA Length | 15 | Number of candles in the exponential average the closing price is measured against. A longer one crosses less often and holds a trade through more noise. |
| Order Volume | 1 | Order size, in lots. |
| Trailing Stop, % | 1.5 | Trailing stop distance, in percent. It is measured from the best price reached since the position was opened, not from the entry price. |

## Diagram Details

- The trailing level follows the price the block is given, and here that price is a close. The stop therefore sits where the candles closed and not where their wicks reached, so an intrabar spike neither drags the level along nor knocks the trade out.
- A finer trail is one link away: feed the Price socket from a Level 1 block reading the last trade price, or hand the order book to the Market depth socket instead, and the same stop is repriced on every quote rather than once every four hours.
- Every own fill goes into Position protection, the flattening ones included. A closing fill nets its running position back to zero and disarms the stop; without it the block would go on watching a position that no longer exists and would eventually protect it by opening the opposite one.
- The average publishes only formed and final values, so the opening bars, where it is still filling up, cannot produce a crossing of their own.
- Order volume is a single exposed number shared by both entry blocks. The closing block takes no volume at all, because a close-position order sizes itself from whatever is open.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
