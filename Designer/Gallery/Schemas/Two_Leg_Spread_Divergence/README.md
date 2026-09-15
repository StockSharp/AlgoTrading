# Two-Leg Spread Divergence Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two instruments that usually move together sometimes move by different amounts, and the one that fell behind tends to catch up. The diagram measures how far each of the two has travelled over the same number of bars, subtracts one figure from the other, and when the gap opens wide enough it buys the leg that lagged and sells the leg that ran ahead at the same moment. Both legs are given back when the gap closes again.

![schema](schema.svg)

## Strategy Overview

- A Security variable names the second instrument, so the pair is a setting rather than a wiring decision, and that same variable is what points the second candle series and every second-leg order block at it. The first leg is the instrument the strategy itself is set to.
- Both candle series are set to finished candles only, so an unfinished bar can never move a decision or date an order.
- Sync holds one line per instrument and lets both candles out together on the five-minute beat. Two feeds arrive independently, and only after that hold do the two candles belong to the same bar - which is the one thing that makes comparing the two instruments meaningful.
- For each released candle a converter takes the close, a Previous value block holds the candle a fixed number of bars back, and a second converter reads the close out of that older candle. The block holds the candle itself and the field is read afterwards, which is the order that gives a value on every bar.
- Two formulas turn each pair of prices into one number: how far that leg has travelled since the older bar, in percent of the older price.
- Two variables latch those two percentages onto the traded candle: each holds the last figure the pair produced and releases it when a bar on the traded instrument finishes, so every comparison, gate and order downstream carries the clock of the instrument the orders go to.
- A formula subtracts the second leg's travel from the traded leg's, which is the divergence, and another takes its size regardless of sign for the exit. Comparisons read the divergence against a symmetric band, both leg figures against zero, and the position against zero; two Ands and an Or turn the two sign readings into a single correlation flag.
- Three logical conditions assemble the decisions, and six position blocks carry them out: two open a pair in each direction, one order per instrument, and two close both legs. Each opening block is set to act only from a flat position on its own instrument, so a signal that repeats while a pair is running adds nothing to it.

## Entry and Exit Rules

- **Long entry**: The divergence sits below the lower edge of the band - the traded leg has fallen behind the second one - both legs travelled the same way over the shift, and the traded leg is flat. On that one signal two blocks fire together: one buys the traded leg volume at market on the strategy instrument, the other sells the second leg volume at market on the named instrument.
- **Short entry**: The divergence sits above the upper edge of the band - the traded leg has run ahead of the second one - both legs travelled the same way over the shift, and the traded leg is flat. The mirrored pair of blocks sells the traded leg volume and buys the second leg volume, both at market.
- **Exit**: Both legs are given back together when the gap they were opened on has closed: the size of the divergence, taken without its sign, falls under the exit threshold while a pair is open. Two closing blocks fire on that single signal, one per instrument, and each works out its own size from what it finds open, so neither needs a volume of its own and a closing block fired on a flat instrument simply does nothing. There is no money target, no stop-loss and no time limit here; the two legs coming back together is the whole exit.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Second Instrument | TONUSDT@BNBFT | The instrument the second leg is traded on. It has to exist in the connected data and be a real tradable instrument: orders are sent to it, not merely read from it. |
| Traded Candles | 00:05:00 | Length of the candles on the strategy's own instrument. The decisions and the orders run on this beat. |
| Second Leg Candles | 00:05:00 | Length of the candles on the second instrument. Keep it equal to the traded series, or the two legs are measured over different spans of time and the difference between them means nothing. |
| Sync Interval | 00:05:00 | The beat the hold releases both instruments on. Match it to the candle length. |
| Traded Leg Shift | 12 | How many bars back the traded leg's travel is measured from. Twelve five-minute bars is one hour. |
| Second Leg Shift | 12 | The same count for the second leg. Keep the two equal: two different spans make the subtraction meaningless. |
| Divergence Threshold, % | 0.3 | How far apart the two travels have to be, in percent, before the pair is worth opening. The band is symmetric, so this one value sets both edges. |
| Exit Threshold, % | 0.1 | How close the two travels have to come back, in percent, before both legs are given back. Keep it below the entry threshold, or a pair is closed on the bar it was opened on. |
| Traded Leg Volume | 1 | Order size on the strategy instrument, in lots. |
| Second Leg Volume | 10 | Order size on the second instrument, in lots. The two sizes are set independently, so the ratio between the legs is a setting rather than something worked out from prices - pick it to match the price scales of the two instruments, and check it against the volume step of the second one, or the order will be rounded down. |

## Diagram Details

- Sync is what pairs the bars, but nothing is ordered off its release. Two feeds do not finish at the same instant, and a set released by the hold carries the earlier of the two times; an order dated before the current time is refused. The two variables that latch the figures onto the traded candle are what keep the whole downstream on a single clock.
- The consequence of that latch is worth knowing: the figures a decision is taken on are the last complete pair, so the pair opens on the bar after the one that produced the reading rather than inside it.
- Every constant - zero, both thresholds and both volumes - is triggered by the traded candle. A comparison needs both of its values to arrive again for every evaluation, and a constant that is never re-sent quietly stops the condition it feeds without any sign of it.
- The lower edge of the band is a formula over the threshold rather than a second constant, so the band stays symmetric whatever the threshold is set to, and there is one number to change instead of two that can drift apart.
- The position block reads the traded instrument only, and its value is latched onto the bar in the same way as the leg figures. Both legs are opened and closed by the same signals, so the state of the traded leg stands for the state of the pair.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
