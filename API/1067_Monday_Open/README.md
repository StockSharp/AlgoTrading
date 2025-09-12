# Monday Open Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys at the beginning of the week and closes the position on Tuesday's close within a specified year range.

## Details

- **Entry Criteria**:
  - **Long**: open a long position on Monday.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - close the long position on Tuesday.
- **Stops**: No.
- **Default Values**:
  - `StartYear` = 2023.
  - `EndYear` = 2025.
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
