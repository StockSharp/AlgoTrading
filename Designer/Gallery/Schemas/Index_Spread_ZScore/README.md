# Delta Neutral Spread Z-Score Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two instruments that normally move together sometimes drift apart, and the gap between them tends to close again. The diagram divides one price by the other to obtain a synthetic instrument, measures how far that ratio has strayed from its own average in units of its own volatility, and opens both instruments at once in opposite directions: long the cheap side against short the expensive one. When the ratio comes back to where it usually sits, both legs are released. Every entry is also spoken aloud as a line of text carrying the reading that caused it.

![schema](schema.svg)

## Strategy Overview

- An index block builds one synthetic security out of the two real ones by dividing the price of the first by the price of the second, and a candle series is subscribed to that synthetic security, so the spread arrives as ready-made candles instead of being assembled by hand from two feeds.
- A moving average and a standard deviation run over the spread candles, and a converter takes their close price. Those three numbers are everything the decision needs: where the spread is, where it usually sits, and how wide it usually swings.
- One formula turns them into a z-score, the distance from the close to the average divided by the deviation, and a mirrored formula produces the same figure with the sign flipped, so a single entry threshold and a single exit threshold serve both directions without a second pair of constants.
- Two more candle series run on the two instruments that are actually traded, and two variables latch the readings onto the traded one: each holds the last number the spread produced and releases it when a traded candle finishes, so every decision carries the clock of the instrument the orders go to.
- Two position blocks, one per instrument, report what is open. Their values are latched on the same traded bar and compared with zero, which gives the diagram four plain answers: each leg is either flat or open.
- Comparisons against the entry threshold say whether the spread is far below or far above its average, and a logical condition combines that with both legs being flat. Only then do four position blocks fire together, buying one instrument and selling the other for the same size.
- Two comparisons against the exit threshold, joined by a logical condition, say that the reading is small in absolute terms, that is, the spread is back near its average. Each leg has its own release gate, so a leg that is already flat is never asked to close and a leg that is still open always is.
- At the moment of entry a variable captures the z-score that caused it, a string formatter writes it into a sentence, and a notification block puts that sentence in the strategy log. The chart panel draws both traded instruments, the synthetic spread series, the average, the deviation, and every order and fill.

## Entry and Exit Rules

- **Long entry**: The mirrored z-score is above the entry threshold, meaning the spread sits more than that many standard deviations below its own average, and both legs are flat. The diagram buys the traded instrument and sells the hedge instrument, both at market and both for the order volume.
- **Short entry**: The z-score is above the entry threshold, meaning the spread sits more than that many standard deviations above its own average, and both legs are flat. The diagram sells the traded instrument and buys the hedge instrument, both at market and both for the order volume.
- **Exit**: Both legs are released when the absolute z-score falls under the exit threshold, that is, when the spread has come back inside a narrow band around its average. The two closing blocks carry no side of their own and are set to close the position: each works out on its own which way to trade and for how much, so the same pair of blocks unwinds a long spread and a short one. In addition, the traded leg carries a percentage stop-loss measured from its fill price. That stop covers one leg only: a paired position cannot be closed by a single protective order, so the hedge leg keeps its own release gate and goes when the spread returns.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | The expression the synthetic security is built from: the price of the traded instrument divided by the price of the hedge instrument. Both instruments have to exist in the connected data, and both of them are traded. |
| Spread Candles | 00:05:00 | Length of the candles the spread series is built on, and therefore the bar the average and the deviation are measured over. |
| Traded Leg Candles | 00:05:00 | Length of the candles the traded leg is followed on. This is the clock the whole diagram runs by, so keep it equal to the spread series or the latched readings will be older than the bar they are read on. |
| Hedge Leg Candles | 00:05:00 | Length of the candles the hedge leg is followed on. The series exists so the hedge instrument's prices reach the run and appear on the chart; keep it equal to the other two. |
| Average Length | 20 | Number of spread candles in the moving average the z-score is measured from. |
| Deviation Length | 20 | Number of spread candles in the standard deviation the z-score is divided by. Keep it equal to the average length unless you deliberately want a fast level against a slow width. |
| Entry Z-Score | 1.5 | How many standard deviations the spread has to sit away from its average before the pair is opened. Raising it makes entries rarer and the stretch they are taken on wider. |
| Exit Z-Score | 0.5 | How close to its average the spread has to come back before both legs are released. It is an absolute figure and covers both sides, so the same number closes a long spread and a short one. |
| Order Volume | 1 | Size of each leg, in lots. Both legs are sent for the same size. |
| Stop Loss, % | 1.5 | Stop-loss for the traded leg, in percent of the price it was filled at. It protects one leg only; the hedge leg has no stop of its own. |

## Diagram Details

- The synthetic security is a ratio rather than a difference. A difference between two instruments of very different price levels is dominated by the larger one, and the z-score would then measure that one instrument instead of the relationship between the two.
- The two series do not finish at the same instant: a synthetic security is assembled from two feeds and its candle is closed a little after a plain one. Latching the readings on the traded candle keeps the whole diagram on a single clock; releasing both series together instead would date every order by the earlier of the two, and an order dated before the current time is refused.
- The constants are triggered by the traded candle. A comparison needs both of its values to arrive again for every evaluation, so a constant that is never re-sent silently stops the condition it feeds.
- The division that produces the z-score is floored at a very small positive number, so a stretch of perfectly still prices cannot divide by a zero deviation and stop the run.
- Each leg is watched by its own position block and gated separately, so if the stop takes the traded leg out first the hedge leg is still released on its own condition rather than left standing until the next entry.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
