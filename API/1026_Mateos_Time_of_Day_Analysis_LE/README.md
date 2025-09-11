# Mateo's Time of Day Analysis LE
[Русский](README_ru.md) | [中文](README_cn.md)

Opens a long position during a specified intraday window and closes it later in the day.

This strategy is useful for exploring time-of-day effects.

## Details

- **Entry Criteria**: Time reaches `StartTime` within the `From`-`Thru` date range.
- **Long/Short**: Long only.
- **Exit Criteria**: Time reaches `EndTime` (before 20:00).
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `StartTime` = 09:30
  - `EndTime` = 16:00
  - `From` = 2017-04-21
  - `Thru` = 2099-12-01
- **Filters**:
  - Category: Time-based
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Low
