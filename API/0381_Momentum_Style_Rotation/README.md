# Momentum Style Rotation Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This Python strategy rotates among a set of factor exchange​-traded funds (ETFs) and a broad market ETF. At the end of each month the ETFs are ranked by their trailing three-month total return. The portfolio then invests entirely in the top ranked fund for the following month to harvest medium-term momentum.

The approach always holds a single ETF and re-evaluates monthly. Daily candles are used for calculations and all rebalancing trades are executed at the market price.

## Details

- **Universe**: list of factor ETFs and a benchmark ETF.
- **Signal**: compute 63-day (three-month) total return and select the strongest instrument.
- **Rebalance**: first trading day of each month.
- **Positioning**: fully long the selected ETF, all others flat.
- **Risk control**: orders skipped when the required trade value falls below `MinTradeUsd`.
