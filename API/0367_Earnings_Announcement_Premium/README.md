# Earnings Announcement Premium
[Русский](README_ru.md) | [中文](README_zh.md)

The **Earnings Announcement Premium** strategy buys stocks a few days before earnings announcements and exits shortly after the release.

## Details
- **Entry Criteria**: Buy `DaysBefore` days prior to earnings.
- **Long/Short**: Long only.
- **Exit Criteria**: Sell `DaysAfter` days after earnings.
- **Stops**: No.
- **Default Values**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Event-driven
  - Direction: Long
  - Indicators: Calendar
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
