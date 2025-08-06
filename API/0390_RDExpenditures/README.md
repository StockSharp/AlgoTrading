# R&D Expenditures Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This cross-sectional strategy ranks stocks by their ratio of research-and-development (R&D) expenses to market value. At the start of each month the top quintile of firms with the highest R&D intensity is bought while the bottom quintile is sold short, betting that heavy R&D spending predicts future outperformance.

Weights are assigned equally within each side and rebalanced monthly using daily price data.

## Details

- **Universe**: list of stocks with R&D data.
- **Signal**: R&D expenditures divided by market capitalization.
- **Portfolio**: long highest quintile, short lowest quintile.
- **Rebalance**: monthly.
- **Risk control**: trades skipped when order value below `MinTradeUsd`.
