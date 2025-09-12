# How To Set Backtest Time Ranges
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy demonstrates restricting trading to specific date and intraday time windows. It enters long when a fast SMA crosses above a slow SMA and exits on the opposite crossover.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Fast SMA crosses above slow SMA within the selected date and entry time ranges.
- **Exit Criteria**: Fast SMA crosses below slow SMA within the selected date and exit time ranges.
- **Stops**: None.
- **Default Values**:
  - `FastLength` = 14
  - `SlowLength` = 28
  - `FromDate` = 2021-01-01
  - `ThruDate` = 2112-01-01
  - `EntryStart` = 00:00
  - `EntryEnd` = 00:00
  - `ExitStart` = 00:00
  - `ExitEnd` = 00:00
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: SMA
  - Complexity: Low
  - Risk level: Medium
