# Mutual Fund Momentum Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This strategy rotates quarterly among a set of mutual funds. At the end of each quarter the funds are ranked by their trailing six-month performance. Capital is allocated to the top fund for the next quarter, allowing long-term investors to follow persistent momentum in actively managed products.

Only one fund is held at a time. Daily price data is used, and rebalancing occurs during the first three trading days of January, April, July, and October.

## Details

- **Universe**: list of mutual funds.
- **Signal**: 126-day (six-month) total return ranking.
- **Rebalance**: quarterly on the first trading days of the new quarter.
- **Positioning**: fully long the highest-ranked fund.
- **Risk control**: skip trade when order value below `MinTradeUsd`.
