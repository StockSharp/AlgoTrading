# Pairs Trading Country ETFs Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This mean-reversion strategy trades a pair of country ETFs based on the z-score of their price ratio. When the ratio deviates beyond a threshold the system enters a long/short position expecting the spread to revert toward its average.

The price ratio is tracked with a rolling window and positions are closed when the z-score crosses the exit level.

## Details

- **Universe**: exactly two country ETFs.
- **Signal**: z-score of rolling price ratio exceeding `EntryZ`.
- **Exit**: close when z-score reverts to `ExitZ`.
- **Data**: daily candles, 60-day window by default.
- **Risk control**: orders skipped if trade value below `MinTradeUsd`.
