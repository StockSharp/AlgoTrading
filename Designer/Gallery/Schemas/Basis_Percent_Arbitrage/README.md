# Basis Percent Arbitrage Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A pair has two prices: the one it is worth and the one it can be dealt at. The diagram builds the first as a synthetic instrument and its moving average, reads the second out of the two order books, states the distance between them as a percentage, and opens both legs when that distance grows past a threshold.

![schema](schema.svg)

## Strategy Overview

- An index block builds one synthetic security by dividing the price of the first instrument by the price of the second, a candle series is subscribed to it, and a moving average over those candles is the fair value of the pair.
- Two order book blocks supply the prices the pair can actually be dealt at: a converter takes the best bid of the first instrument, another takes the best ask of the second.
- A book changes many times inside a single bar, so the two best prices are not fed into the conditions directly: two variables hold the last quote each and release it when a traded candle finishes, and a formula divides one by the other into the executable ratio.
- A sync block holds the executable ratio and the ratio candle of the same stamp and lets them out together, so the fair value and the dealable price that are compared always belong to the same bar.
- A formula turns the released pair into a single number: the distance from the executable ratio to the fair average, in percent of the average. That number is the basis.
- A variable latches the basis onto the traded candle, and every comparison downstream is evaluated on that bar, so each order is dated by the bar it was decided on.
- The basis is compared with the entry threshold and with the same threshold negated, which gives one gate per direction, and with a much narrower band around the fair value, whose two answers a logical condition joins into the return signal.
- A delay block armed by either entry counts finished traded bars and raises a single flag once the hold limit is reached; a combination merges that flag with the return signal into one exit stream, and two closing blocks take the pair off, one per instrument.

## Entry and Exit Rules

- **Long entry**: The executable ratio is below the fair average by more than the minimum basis: the diagram buys the first instrument and sells the second, one order volume in each leg. Each leg is set to open only from a position of its own that is flat, so a signal that repeats while the pair is already on adds nothing to it.
- **Short entry**: The executable ratio is above the fair average by more than the minimum basis: the diagram sells the first instrument and buys the second, one order volume in each leg. The same flat-only condition guards each leg separately.
- **Exit**: Both legs are taken off on whichever comes first: the basis returns inside the narrow band around the fair value, or the delay block has counted the hold limit of finished traded bars since the entry that armed it. Both reasons arrive at the same trigger through one merging block. The closing blocks carry no side and work out their own volume from what is open, and a closing block fired on a flat leg simply does nothing.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Traded Candles | 00:05:00 | Length of the candles the decisions and the orders are placed on. |
| Index Expression | BTCUSDT@BNBFT/TONUSDT@BNBFT | The expression the synthetic security is built from: the price of the first instrument divided by the price of the second. Both instruments have to exist in the connected data, and both of them are traded. |
| Ratio Candles | 00:05:00 | Length of the candles the fair value is built on. Keep it equal to the traded series, or the two will not meet in one set. |
| Sync Interval | 00:05:00 | Bucket the sync block gathers by. Keep it equal to the candle length: a shorter bucket splits the pair of values that belong together, a longer one joins values from different bars. |
| Fair Average Length | 20 | Number of ratio candles in the moving average the fair value is taken from. |
| Minimum Basis, % | 0.5 | How far the dealable ratio has to stand from the fair value, in percent, before both legs are opened. |
| Exit Basis, % | 0.1 | How close the basis has to come back to the fair value, in percent, before the pair is taken off. |
| Volume Per Leg | 1 | Order size of each leg, in lots. Both legs are sent the same size. |
| Max Hold Bars | 72 | How many finished traded bars the pair may be held for before it is closed regardless of the basis. |

## Diagram Details

- The order book is never read straight into a condition. It updates many times per bar, while the conditions live on the bar; the two latching variables are what puts both on one clock, and their trigger is the traded candle rather than the book.
- Nothing that places an order is triggered from a sync output. A released set is dated by the earliest of its members, and an order dated before the current time is refused, so the sync feeds the arithmetic while the traded candle drives every trigger.
- Both candle series are set to finished candles only. An update of a forming bar would date an order by the moment the bar opened, which is already in the past by the time the bar is decided on.
- The constants are re-sent on every traded candle. A comparison needs both of its values to arrive again for each evaluation, so a constant that is sent once quietly stops the condition it feeds.
- The lower thresholds are not constants of their own but formulas over the upper ones, so both bands stay symmetrical whatever the thresholds are set to, and the hold limit is counted in finished bars rather than in clock time.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
