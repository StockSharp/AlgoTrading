# Candle Streak Profit Lock Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Four candles that all close the same way are a run, and this diagram trades in the direction of the run. Getting in is the easy half; the interesting half is getting out, because the diagram does not wait for a price level to decide that. It watches the money in the open position, and the first time that figure reaches a set amount the position is closed and the profit is booked. A latch makes sure that command is sent once and not on every update of the result.

![schema](schema.svg)

## Strategy Overview

- Finished four-hour candles are read once and reused: the current candle goes straight to a close and an open converter, and three previous-value blocks hand back the candles one, two and three bars behind, each with a close and an open converter of its own.
- Eight comparisons turn those four pairs into the colour of each bar: close above open is a rising candle, close below open is a falling one, and a bar that closes exactly where it opened is neither, so it fails both tests and cannot extend either run.
- One logical AND collects the four rising answers and a second collects the four falling ones; each emits true only when all four bars in the window agree, which is what a streak is.
- The position block compared with zero says whether the diagram is flat, long or short, and every gate downstream is a logical AND of one streak and one position state.
- Both entries are market orders of a fixed volume and carry the open-position condition, so a streak that keeps running while a position is already open cannot pile a second order on top of the first.
- Entry fills are joined into one line and handed to position protection, which carries a take-profit and a trailing stop as a percentage of the fill price and quotes them for as long as the position lives.
- The P&L block emits the open result of the running position on a beat of its own, and a comparison holds that figure against the profit lock; the answer goes into a flag, which passes the first true through and then stays set, so exactly one closing command is sent.
- A streak that forms against an open position closes it at market, and the flag is re-armed by either entry gate, so each new position starts with its lock ready again.

## Entry and Exit Rules

- **Long entry**: Four finished candles in a row close above their own opens while the position is flat: position modify buys the order volume at market.
- **Short entry**: Four finished candles in a row close below their own opens while the position is flat: position modify sells the order volume at market.
- **Exit**: Three ways out, and the position leaves by whichever arrives first. The profit lock closes it as soon as the open result reaches the set amount; because the flag has passed that signal through once, later updates of the same result send nothing. Position protection closes it on its take-profit or on its trailing stop, the stop following price once the trade is in profit. A streak in the opposite direction closes it at market as well, and that closing order is all that happens on that bar: the entry gates require a flat position, so a reversal is opened by the next streak signal that finds the diagram empty rather than by the same one that emptied it.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candle Type | 04:00:00 | Time frame of the candle series the streak is counted on; the four bars of a run are four candles of this length. |
| Order Volume | 1 | Order size, in lots, sent by each entry; it also scales the open result the profit lock is compared with. |
| Take Profit | 1% | Take-profit distance from the fill price, in percent. |
| Stop Loss | 1% | Stop-loss distance from the fill price, in percent; the stop trails, so it follows price once the trade moves in favour and never moves back. |
| Profit Lock | 200 | Open result, in the money of the portfolio, at which the position is closed and the profit is booked. |

## Diagram Details

- The length of the run is built into the diagram rather than set as a number: four bars means three previous-value blocks and four inputs on each streak gate. A run of five is three more blocks and one more input per gate; a run of three is one block fewer.
- Each previous-value block is typed as a candle and reads the candle series directly, and the close and open are taken from what it returns. Taking a price first and then asking for its previous value is the other way round, and it is the one that leaves the comparison without an operand.
- The profit lock is a figure in the money of the portfolio, not a distance in price, so it depends on the order volume as much as on the instrument. Doubling the volume halves the move needed to reach it; changing to an instrument of a different price scale changes it entirely.
- The lock and the take-profit are two answers to the same question and the tighter one wins. Set the lock so that it fires before the take-profit and the take becomes the ceiling that is reached only when a bar jumps past the lock; set it wide and the take-profit is the ordinary exit and the lock is the safety net behind it.
- Everything the diagram sends is a market order, and the candle series is subscribed as finished bars only. A signal built from a bar that is still forming carries the opening time of that bar, and an order stamped with a time already in the past is refused.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
