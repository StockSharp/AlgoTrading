# Earnings Announcements With Buybacks
[Русский](README_ru.md) | [中文](README_zh.md)

The **Earnings Announcements With Buybacks** strategy buys stocks running active buyback programs a few days before their earnings announcements and exits shortly after the report.

## Details
- **Entry Criteria**: Buy `DaysBefore` days before earnings if the company has an active buyback.
- **Long/Short**: Long only.
- **Exit Criteria**: Sell `DaysAfter` days after the earnings date.
- **Stops**: No.
- **Default Values**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Event-driven
  - Direction: Long
  - Indicators: Buyback + Calendar
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
