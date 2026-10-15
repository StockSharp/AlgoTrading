# Power Hour Money Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy trades during selected New York sessions and opens positions when all major time frames agree.
A long position is taken when month, week, day and hour candles close higher than they open.
A short position is taken when all of them close lower.
Optional trailing stops protect profits, and positions can be closed at 16:45.

## Details

- **Entry**: long when all time frames are green, short when all are red.
- **Session filter**: NY 9:30-11:30, extended 8:00-16:00 or all sessions.
- **Trailing stop**: percent-based for long and short sides.
- **End of day**: optional closing of all positions at 16:45.

## Calculation and execution

C# and Python use the same causal model: after each finished base candle, its close is the currently known close of the developing month, week, day and hour. Compare it strictly with each period's open. Mixed colors or equality hold the position. These are developing higher-timeframe candles; future month/week/day/hour closes are never read.

Month, Monday-based week and day follow the New York calendar, including daylight saving time. Hours are keyed by UTC to distinguish the repeated autumn hour. After an observed period change, the first available base candle supplies the new open. A period already in progress when history starts remains unknown until its next boundary; its missing open is not replaced by the first observed price. All four opens must be known before entry.

Default `CandleType` is five minutes. `SessionStart=09:30` and `SessionEnd=11:30` use New York market time, start inclusive and end exclusive. Set them to `08:00` and `16:00` for the extended session, or set `IsAllSessions=true`. `IsAllSessions`, `IsTrailingEnabled` and `IsEndOfDayCloseEnabled` default to `false`; `LongTrailingPercent=2`, `ShortTrailingPercent=2`, `EndOfDayTime=16:45`.

An aligned color enters with `Volume` when flat or fully reverses with `Volume + abs(Position)`. An outstanding order blocks additional orders. There is no cooldown. Optional trailing starts from the actual fill: long closes when the current bid reaches `highest observed bid * (1 - LongTrailingPercent / 100)`; short closes when the current ask reaches `lowest observed ask * (1 + ShortTrailingPercent / 100)`. Fill prices also update the favorable extreme. Both percentages must be strictly between 0 and 100. A reversal resets the extreme.

Optional end-of-day closing submits a market order for the entire position on the first market-time callback at or after the cutoff and blocks entries until New York midnight. Quote callbacks and a one-minute market-time timer check it independently of candle warmup and sessions. Trailing and daily closing require live market-time events; they do not promise an exact fill price across gaps.
