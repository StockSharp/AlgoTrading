# Power Hour Money Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades during selected New York sessions and opens positions when all major time frames agree.
A long position is taken when month, week, day and hour candles close higher than they open.
A short position is taken when all of them close lower.
Optional trailing stops protect profits, and positions can be closed at 16:45.

## Details
- **Entry**: long when all time frames are green, short when all are red.
- **Session filter**: NY 9:30-11:30, extended 8:00-16:00 or all sessions.
- **Trailing stop**: percent-based for long and short sides.
- **End of day**: optional closing of all positions at 16:45.
