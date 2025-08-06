# WTI Brent Spread
[Русский](README_ru.md) | [中文](README_cn.md)

The trade targets the price differential between WTI and Brent crude oil. When the spread deviates from historical norms, the system bets on mean reversion by longing one grade and shorting the other.

Positions roll with the front‑month futures and are closed when the spread converges.

## Details

- **Data**: Front‑month WTI and Brent futures prices.
- **Entry**: Long cheaper grade and short expensive when spread > threshold.
- **Exit**: Close when spread returns to average or at contract roll.
- **Instruments**: Crude oil futures.
- **Risk**: Dollar‑neutral with stop on spread widening.

