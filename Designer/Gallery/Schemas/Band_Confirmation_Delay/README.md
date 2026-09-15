# Band Confirmation Delay Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A signal does not have to be acted on the second it appears. This diagram builds a volatility band above a moving average, and when the close pushes through it the diagram does not buy. It arms a delay block instead, waits a fixed number of candles, and only then asks the same question again: is the price still above the band? The answer at that moment, not the one that started the wait, decides the trade.

![schema](schema.svg)

## Strategy Overview

- One candle series of five minutes, finished candles only, feeds everything in the diagram.
- A moving average gives the middle line and a standard deviation gives the width; a formula assembles them into an upper band, middle plus multiplier times deviation.
- Two comparisons reduce the whole picture to two questions: is the close above the band, and is the close below the middle line.
- A logical condition combines the breakout with a position that is not long and arms the entry delay block. While the block is counting, further breakouts are ignored, so one wait is never restarted by the next candle.
- When the delay block finishes counting it emits one impulse, and a second logical condition joins that impulse with a freshly recomputed breakout, an elapsed cooldown and a still-flat position. Without that second condition the diagram would buy blind, on the strength of a signal that could already be gone.
- The exit is built the same way in mirror: a close under the middle line with a long position arms the second delay block, and its impulse, joined with a still-valid return under the middle line, closes the trade.
- A cooldown counter runs alongside: every own fill resets it to zero, every candle increments it up to its cap, and a comparison keeps new entries out until enough candles have passed.
- Entries and exits are market orders through position modify blocks, opening from flat and closing the whole position; the chart panel draws the candles, both indicators, the orders and the fills.

## Entry and Exit Rules

- **Long entry**: The close finishes above the upper band while the position is not long. That arms the entry delay. A configured number of candles later the delay releases its impulse, and if at that moment the close is still above the band, the cooldown since the last fill has run out and the position is still flat, the position modify block buys the order volume at market.
- **Short entry**: There are no short entries. Below the band the diagram simply stands aside; the only order it ever sends against a long position is the one that closes it.
- **Exit**: The close finishes below the middle line while a long position is open, which arms the exit delay. A configured number of candles later the impulse arrives, and if the close is still below the middle line and the position is still long, the closing position modify block sells the whole position at market. There is no stop-loss and no take-profit block: this diagram is about waiting out a confirmation, and the middle line is the only thing that ends a trade.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the single candle series the whole diagram runs on. |
| Middle Line Length | 40 | Length of the moving average that draws the middle line and one half of the band. |
| Deviation Length | 40 | Length of the standard deviation that sets the band width; normally kept equal to the middle line length. |
| Band Multiplier | 1.1 | How many deviations above the middle line the upper band sits. Raise it for rarer, more stretched breakouts. |
| Entry Confirm Candles | 3 | Candles the entry delay counts between the breakout and the re-check that may buy. |
| Exit Confirm Candles | 3 | Candles the exit delay counts between the return under the middle line and the re-check that may close. |
| Cooldown Candles | 8 | Candles that must pass after an own fill before a new entry is allowed. |
| Order Volume | 1 | Order size, in lots, sent on entry; the exit always closes whatever is open. |

## Diagram Details

- The delay block counts values that reach it after it was armed, not consecutive true candles. A repeated arming is ignored while it counts, and the block resets itself once it has fired, so the wait is a genuine pause rather than a running tally of good bars.
- That is exactly why the released impulse is combined with a fresh comparison instead of going straight to the order. The impulse says the wait is over; the comparison says whether the reason for it still exists.
- The confirmation length is deliberately longer than one candle here so the delay block has something visible to do. Set both delays to one candle and the diagram trades on the breakout bar itself, which is the same structure without the pause.
- The candle block emits finished candles only. An update of a forming candle carries the time the bar opened, and an order built from it is dated behind the clock and rejected.
- The cooldown is a counter, not a timer: a variable that holds its value between candles, is reset by own fills and is incremented by a formula capped at the cooldown length, so it cannot drift away and can never block entries permanently.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
