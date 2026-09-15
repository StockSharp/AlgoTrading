# Bands Confirmed Reversion Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A candle that opens outside a volatility band and closes back inside it is the classic picture of a rejected move, and this diagram buys and sells exactly that picture. What makes it more than a one-candle pattern is what stands between the pattern and the order: a price channel whose edge must have been holding its ground, and a counting block that will not release its confirmation until a set number of candles have gone by. Only when the pattern, the channel and the count line up on the same candle does a position open.

![schema](schema.svg)

## Strategy Overview

- A single fifteen-minute candle series feeds the whole diagram, and every value entering it passes through a Final block first, so nothing downstream ever sees a candle that is still forming.
- Three indicators run off that stream: volatility bands around a moving average, a price channel of highs and lows, and an average true range that measures how wide a normal candle is.
- Converters split the bands into an upper and a lower line, split the channel into a top and a bottom, and pull the open, close, high and low out of each closed candle.
- Two Previous-value blocks keep the channel edges from the candle before, and two comparisons ask whether the bottom edge has stopped falling and whether the top edge has stopped rising: those are the diagram's definition of a channel that is holding.
- Each of those two answers arms an N-values block, which then counts the configured number of closed candles and releases a single confirmation impulse; a fresh arming is ignored while a count is running.
- A logical condition brings five things together for the long side: the candle opened below the lower band, it closed back above it, the channel bottom is holding right now, the confirmation impulse has just arrived, and the position is flat. The short side is the same condition mirrored around the upper band and the channel top.
- Both entries are market orders through position-modify blocks set to open only from flat, so a signal that arrives while a trade is running cannot stack a second one on top of it.
- Two Combination blocks gather the exit reasons - a stop distance built from the average true range, and a close beyond the channel of the previous candle - and hand them to the closing position-modify blocks, while a chart panel draws the candles, all three indicators, the orders and the fills.

## Entry and Exit Rules

- **Long entry**: A closed candle opened below the lower band and closed back above it, the channel bottom is at or above where it was on the previous candle, the long counting block has just released its confirmation, and the position is flat. The position-modify block then buys the order volume at market. The count is what spaces the trades out: after each release the block re-arms on the next candle whose channel bottom is still holding, so the same pattern on the very next candle does not produce a second entry.
- **Short entry**: The mirror image. A closed candle opened above the upper band and closed back below it, the channel top is at or below where it was on the previous candle, the short counting block has released its confirmation, and the position is flat. The position-modify block sells the order volume at market.
- **Exit**: Each side has its own Combination block holding two kinds of reason. The first is a volatility stop: the long is closed when the candle's low drops below the lower band minus the average true range times the stop multiplier, and the short is closed when the candle's high climbs above the upper band plus the same distance. Because the band moves with the market, that stop trails on its own without the diagram having to remember an entry price. The second reason is a channel breakout: a close above the channel top of the previous candle, or a close below the channel bottom of the previous candle, ends the trade on either side - upward it takes the profit out of a long, downward it takes the loss out of it, and the reverse for a short. Both closing blocks are set to close the position, so each only acts on the side it belongs to and always sends the whole size.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:15:00 | Time frame of the single candle series the whole diagram runs on. A shorter frame gives more patterns and more trades, a longer one fewer and slower. |
| Bollinger Length | 100 | Number of candles the volatility bands are averaged over. It also sets the warm-up: nothing is traded until that many candles have passed. |
| Bollinger Width | 1 | How many standard deviations away from the average each band sits. Kept narrow here so that candles regularly open outside a band and close back inside; widen it for rarer and more extreme rejections. |
| Donchian Length | 100 | Number of candles the price channel spans. A long channel makes its edges slow, which is what turns 'the edge stopped moving against us' into a meaningful filter. |
| ATR Length | 21 | Number of candles the average true range is measured over. It sets the unit the stop distance is expressed in. |
| Long Confirm Candles | 5 | Candles the long counting block waits between arming and releasing its confirmation. One candle removes the wait entirely and trades every pattern; larger values thin the entries out. |
| Short Confirm Candles | 5 | Candles the short counting block waits. It is a separate setting so the two sides can be tuned against each other. |
| ATR Stop Multiplier | 2 | How many average true ranges below the lower band the long stop sits, and above the upper band the short stop sits. Lower it for tighter, more frequent exits. |
| Order Volume | 1 | Order size, in lots, sent on entry. The exits always close whatever is open and take no size of their own. |

## Diagram Details

- The candle block is set to finished candles only, and the Final block behind it enforces the same rule inside the diagram. An update of a forming candle carries the time the bar opened, and an order built from such a value is dated behind the clock and refused, so the entire logic is kept on closed candles.
- The N-values block counts values that reach it after it was armed; it does not verify that the arming condition stayed true throughout. That is why the same channel comparison that arms it is also wired straight into the entry condition: the impulse says the wait is over, and the live comparison says whether the reason for it still exists.
- The channel edges used for the exit are taken one candle back. The current top of a highs-and-lows channel already contains the current candle's own high, so a close can never rise above it; against the previous candle's edge, a breakout is a real event.
- The stop is anchored to the volatility band rather than to the price the trade opened at. A diagram has no memory of an entry price unless a variable is added to hold it, and the band is where the entry happened anyway, so it gives the same protective distance and moves along with the market.
- The position is guarded twice, on purpose: the flat comparison sits inside the entry condition, and the entry blocks themselves are set to open only when the position is zero. The first keeps the diagram readable, the second is what actually stops a duplicate order if the two arrive out of step.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
