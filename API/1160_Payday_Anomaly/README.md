# Payday Anomaly Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy opens a long position on selected paydays (1st, 2nd, 16th, 31st of each month) and closes the position on the following day.

## Details

- **Entry Criteria**:
  - **Long**: open a long position on selected days of the month.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - close the long position when the day is not selected.
- **Stops**: No.
- **Default Values**:
  - `Trade1st` = true.
  - `Trade2nd` = true.
  - `Trade16th` = true.
  - `Trade31st` = true.
  - `CandleType` = TimeSpan.FromDays(1).TimeFrame().
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
