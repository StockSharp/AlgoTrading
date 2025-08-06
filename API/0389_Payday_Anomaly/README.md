# Payday Anomaly Strategy

[Русский](README_ru.md) | [中文](README_zh.md)

This strategy exploits the "payday" effect by holding a broad market ETF around typical salary payment dates. The ETF is owned from two trading days before month-end through the third trading day of the new month, capturing inflows from paycheck contributions.

The rest of the month the portfolio is in cash. Daily candles determine the window and market orders adjust the position.

## Details

- **Instrument**: broad market ETF.
- **Window**: from two days before month end to third trading day of next month.
- **Positioning**: long during window, flat otherwise.
- **Data**: daily candles.
- **Risk control**: trade skipped if order value below `MinTradeUsd`.
