# Pairs Trading Stocks Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This simplified pairs trading strategy operates on multiple stock pairs. For each pair the price ratio is tracked over a rolling window and its z-score is computed. When the z-score exceeds an entry threshold a long/short trade is opened; positions are closed when the z-score reverts.

The algorithm supports trading multiple independent pairs simultaneously.

## Details

- **Universe**: list of stock pairs.
- **Signal**: z-score of price ratio crossing `EntryZ`.
- **Exit**: close when z-score reaches `ExitZ`.
- **Data**: daily candles with 60-day lookback by default.
- **Risk control**: trades skipped when order value below `MinTradeUsd`.
