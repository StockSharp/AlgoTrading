# Month12 Cycle Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This Python strategy implements the 12​-month cycle anomaly. Stocks are ranked by the return they earned one year ago over the corresponding calendar month. Each month the top decile is bought and the bottom decile is sold short, creating a market-neutral portfolio based on lagged annual performance.

The system uses daily data to approximate monthly closes and rebalances at the start of every month. Position sizes are scaled to keep dollar exposure balanced across long and short sides.

## Details

- **Universe**: user defined list of securities.
- **Signal**: sort by percentage change from the same month one year earlier.
- **Portfolio**: long top decile, short bottom decile with leverage per leg set by `Leverage`.
- **Rebalance**: monthly.
- **Data**: daily candles aggregated into month-end prices.
