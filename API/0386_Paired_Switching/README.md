# Paired Switching Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This quarterly strategy switches between two ETFs, always holding the one with the higher prior-quarter return. On the first trading day of each quarter the trailing 63-day performance of both funds is compared and the entire portfolio is allocated to the leader.

Daily candles drive the calculations and trades are executed with market orders. The approach aims to capture relative strength between closely related ETFs.

## Details

- **Instruments**: two user-specified ETFs.
- **Signal**: compare prior-quarter total returns.
- **Rebalance**: first trading day of each quarter.
- **Positioning**: fully invested in the better-performing ETF.
- **Risk control**: no trade if order value below `MinTradeUsd`.
