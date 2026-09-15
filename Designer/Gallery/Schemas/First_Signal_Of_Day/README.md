# First Signal Of The Day Strategy Diagram
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Two exponential averages crossing on five-minute candles is an ordinary signal, and on a fast time frame it repeats many times a day. This diagram trades only the earliest one of each direction per calendar day and lets every later crossing pass untouched. The memory that makes that possible is two Flag blocks, and the thing that wipes the memory clean is the strategy clock noticing that the date has changed.

![schema](schema.svg)

## Strategy Overview

- Finished five-minute candles feed a fast and a slow exponential moving average, and a Crossing block turns the pair into a single event: it emits true when the fast average crosses above the slow one and false when it crosses below.
- A logical Not turns that same event into a separate down-cross signal, so one Crossing block serves both directions without a second copy of the averages.
- The Current time block streams the strategy clock into a converter that reads the calendar day out of it, so the diagram has a day number that owes nothing to the candle series.
- A variable holds the day number and releases it when a candle closes, which means it always carries the day the previous candle belonged to; a NotEqual comparison against the live day number is therefore true exactly once, at the first bar after midnight.
- Each direction owns a Flag: the entry signal is its trigger, that day-change pulse is its reset. A Flag passes a trigger through only the first time it is set, so everything after the first accepted signal of the day is silently dropped until the date changes.
- Entries are Position modify blocks set to open only from a flat position, so the two latches spend at most one long and one short per day and never stack size.
- The opposite crossing closes what is open: two logical conditions meet in a Combination that triggers a single Position modify set to close, so neither side needs an exit block of its own.
- Position protection watches the entry fills and can end a trade earlier at a fixed take-profit or stop-loss distance, so a position is never left waiting for a crossing that does not come.

## Entry and Exit Rules

- **Long entry**: The fast average crosses above the slow one while the position is flat. That combination triggers the long Flag, and because a Flag fires only on its first setting, the crossing is executed only if no long has been taken since the date last changed. Position modify then buys at market with the configured volume, and refuses the order outright if anything is already open.
- **Short entry**: The fast average crosses below the slow one while the position is flat. The signal goes through the short Flag, which likewise passes only its first setting of the day, and Position modify sells at market with the same volume. The two Flags are independent, so a day can contain one long and one short, in either order.
- **Exit**: A down-cross while long and an up-cross while short are collected by a Combination that triggers one Position modify set to close the position, which computes the closing volume itself. Position protection runs alongside it on the entry fills and can close the trade earlier at the take-profit or the stop-loss distance. Nothing is reversed in a single step: the position first returns to flat, and the opposite side is opened later, on a crossing that finds the account empty.

## Parameters

| Parameter | Default | Description |
|---|---|---|
| Candles | 00:05:00 | Time frame of the candle series the whole diagram runs on. A slower series means fewer crossings per day and a daily limit that rarely binds. |
| Fast EMA Length | 14 | Length of the fast exponential moving average. |
| Slow EMA Length | 40 | Length of the slow exponential moving average. Keep it comfortably above the fast one, otherwise the two averages cross constantly and only the first crossing of each day survives the latch anyway. |
| Volume | 1 | Size of one entry, in instrument units. Both directions use it. |
| Take Profit, % | 1.5 | Take-profit distance, in percent of the entry price. |
| Stop Loss, % | 1 | Stop-loss distance, in percent of the entry price. |

## Diagram Details

- The day number comes from the clock rather than from the candles, so the reset keeps working on a session with gaps and does not depend on a bar existing at the moment the date rolls over.
- The clock runs on its own tick, far more often than candles close. That is why its value is not compared directly against a stored one: a variable puts it onto the candle beat first, which is what makes the comparison mean 'this candle belongs to a different day than the previous candle'.
- A Flag ignores a false arriving on either socket, so the day-change comparison can stream false for the whole day without disturbing the latch, and a logical condition that evaluates to false costs nothing.
- The entry gate asks for a flat position and the entry block itself refuses to work from anything else, which is deliberate: the latch is spent by the signal, so a signal that could not be filled would burn the day for that direction.
- Both directions share one volume variable and one protection block, so long and short are sized and protected by exactly the same rule.

## Usage

Import the `.json` file into Designer, run it in the backtester on historical data, then adjust the parameters or the blocks themselves to fit your instrument before trading it live.
