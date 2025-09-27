# Supertrend Long-Only for QQQ
[Русский](README_ru.md) | [中文](README_cn.md)

Long-only strategy based on Supertrend indicator and a date range filter.

## Details

- **Entry Criteria**: Price crossing above Supertrend.
- **Long/Short**: Long only.
- **Exit Criteria**: Price crossing below Supertrend.
- **Stops**: No.
- **Default Values**:
  - `AtrPeriod` = 32
  - `Multiplier` = 4.35m
  - `StartDate` = 1995-01-01
  - `EndDate` = 2050-01-01
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: ATR, Supertrend
  - Stops: No
  - Complexity: Basic
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
