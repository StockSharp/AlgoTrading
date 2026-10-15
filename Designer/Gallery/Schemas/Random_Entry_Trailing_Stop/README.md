# Random Entry with Tape Trailing Stop Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Most diagrams spend their blocks deciding when to enter. This one spends almost none on that and everything on the way out. The entry is a coin toss: a Random block draws a number on every closed candle, and which side of a threshold it lands on decides long or short. What happens next is the point of the example — a trailing stop repriced on every executed print of the tape instead of once a bar.

![schema](schema.svg)

## Strategy Overview

- A five-minute candle series is the clock of the diagram. Every closed candle is one draw and one attempt to enter; nothing else advances the entry logic.
- The Random block is triggered by that candle and draws a number between zero and one. A comparison against the threshold gives the long side, and a logical NOT of the same answer gives the short side, so one comparison serves both directions.
- The current position is snapped onto the candle beat by a variable that holds it on its input and releases it on the candle trigger; comparing that held value with zero is the flat check.
- Two logical AND gates combine the coin with the flat check. Both of their inputs arrive on the candle beat, so each gate is decided exactly once per closed candle.
- Both entry blocks carry the open-position condition, so a gate that keeps saying yes cannot add to a position that already exists — the diagram holds one position at a time and never reverses one.
- The tick stream is subscribed alongside the candles, and a converter reads the price out of every executed print.
- Position protection takes the entry fills through a Combination and that print price on its Price socket. Trailing is switched on, so each print that moves the trade further into profit drags the stop behind it and the exit is decided between candles, not on them.
- The chart panel draws the candles, both entry orders, the protective stop order and every fill, so the whole life of a position is readable on one panel.

## Entry and Exit Rules

- **Long entry**: On a closed candle the drawn number is below the threshold and the held position is zero. The long gate passes and Position modify buys the order volume at market.
- **Short entry**: On the same closed candle the drawn number is at or above the threshold — the long comparison inverted by the logical NOT — and the held position is zero. Position modify sells the order volume at market.
- **Exit**: There is no exit signal and no take-profit. Position protection owns the trade from its first fill: it sets the stop 0.5% away from the entry price and, with trailing on, carries it up behind a long and down behind a short as better prices print. The position is closed by a market order the moment a print touches that level, which leaves the next closed candle free to toss the coin again.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series. One closed candle is one coin toss and one attempt to enter. |
| Coin Threshold | 0.5 | The value the drawn number is compared against. At 0.5 the two directions are equally likely; a lower value makes longs rarer, a higher one makes them more common. |
| Order Volume | 1 | Order size, in lots. |
| Trailing Stop, % | 0.5 | Trailing stop distance, in percent of the entry price. |

## Diagram Details

- The Random block draws from the strategy's own random source rather than a global one, so replaying the same history twice gives the same sequence of tosses and the same set of trades.
- The stop is written as a percentage of the entry price rather than as a fixed count of price steps. The same distance in price units means one thing on an instrument quoted near 65 000 and something else entirely on one quoted near 5; the percentage is the only form that survives both.
- Feeding the Price socket from the tape is what makes the exit fine-grained. Priced off a candle close instead, the same stop would only ever be tested twelve times an hour.
- The stop trails continuously: every improvement in price moves it, with no extra distance the price must first cover before the stop is allowed to advance again.
- An entry is attempted on every closed candle while the position is flat, so the diagram is normally holding something and the protective stop always has a position to manage.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
