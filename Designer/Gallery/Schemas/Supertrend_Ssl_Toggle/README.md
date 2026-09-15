# Trend Toggle With A Cooldown Lock
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two exponential averages crossing on a fast candle series produce far more signals than a trend actually changes, and a run of crossings around one price level can fill an account with entries that undo each other. This diagram takes a crossing, spends it, and then locks that side shut for a fixed number of candles. The lock is a Flag block, the countdown that opens it again is a Delay value block, and what starts the countdown is a Combination that merges the fills of both directions into a single line called 'an entry happened'.

![schema](schema.svg)

## Strategy Overview

- Finished candles of one series feed a fast and a slow exponential moving average; nothing in the diagram reacts to a candle that is still forming.
- Two Crossing blocks read the same pair of averages with their inputs swapped, so one of them is true exactly on the bar the fast average crosses above the slow one and the other exactly on the bar it crosses below.
- A Position block compared against zero says whether the account is flat, long or short, and that answer is what turns a crossing into a permitted signal rather than a mere observation.
- Each direction owns a Flag. The entry signal is its trigger, and a Flag lets a trigger through only the first time it is set, so the second and every later signal of that side is dropped in silence.
- Entries are Position modify blocks that open only from a flat account, which keeps a permitted signal and an actually placed order in step with each other.
- Both entry blocks send their fills into one Combination, and that single line arms the Delay value block, hands the fill to position protection and draws the fills on the chart.
- Delay value counts finished candles after the fill and emits one pulse when the count runs out; that pulse is wired into the reset socket of both Flags, and both sides are open for business again.
- An opposite crossing while a position is open is collected by a second Combination and closes the trade, while take-profit and stop-loss distances can end it earlier.

## Entry and Exit Rules

- **Long entry**: The fast average crosses above the slow one on a finished candle while the account is flat. The logical condition that joins those two facts triggers the long Flag; if the Flag is still locked from an earlier long, the signal dies there and nothing is ordered. Otherwise the Flag fires once, Position modify buys at market with the configured volume, and the Flag stays set until the countdown releases it.
- **Short entry**: The fast average crosses below the slow one on a finished candle while the account is flat. The signal passes through the short Flag under the same rule and Position modify sells at market with the same volume. The two Flags are independent, so a locked long side does not stop a short from being taken.
- **Exit**: A down-cross while long and an up-cross while short meet in a Combination that triggers one Position modify set to close, and it works out the closing volume itself. Position protection runs in parallel on the entry fill and can end the trade earlier at the take-profit or the stop-loss distance. Nothing is reversed in a single order: the position returns to flat first, and the opposite side is opened later, by a crossing that finds the account empty and that side unlocked.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series everything in the diagram runs on. It also sets the unit of the cooldown, which is counted in candles of this series. |
| Fast EMA Length | 14 | Length of the fast exponential moving average. |
| Slow EMA Length | 40 | Length of the slow exponential moving average. Keep it clearly above the fast one; averages of similar length cross constantly and the latch would be doing all the filtering. |
| Cooldown Candles | 72 | Number of finished candles a side stays locked after an entry is filled. Raising it thins the trades out, lowering it lets a choppy stretch produce several entries in a row. |
| Volume | 1 | Size of one entry, in instrument units. Both directions use it. |
| Take Profit, % | 1.5 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 1 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- A Flag never emits false. It is a latch, not a gate: it reports the moment it is set, and everything it blocks it blocks by staying silent, which is why the reset socket and not a logical Not is what re-opens a side.
- Delay value arms itself only from an empty state. A fill that lands while the countdown is already running does not extend it, so the pause is measured from the first fill of a series, not from the latest one.
- The countdown is driven by the candle series itself: candles go into the counting input and decrement it, so the pause is expressed in bars and follows the time frame instead of a wall clock.
- The reset pulse opens both sides at once, and each side is locked separately. A direction that never traded during the pause is unlocked by a pulse the other direction paid for, which is the deliberate difference between a per-side latch and a single global timer.
- The entry condition asks for a flat account and the entry block refuses to work from anything else on purpose: a latch is spent by the signal, so a signal that could not be filled would waste the pause for that side.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
