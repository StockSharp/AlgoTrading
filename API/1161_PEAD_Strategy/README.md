# PEAD Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades post-earnings announcement drift after a positive EPS surprise and gap-up.
It enters long on the day after earnings when price gaps up and recent performance is positive,
using an EMA trail, fixed stop/breakeven, and max holding period.

## Details

- **Entry Criteria**: Positive EPS surprise, gap-up after earnings, and prior positive performance.
- **Long/Short**: Long only.
- **Exit Criteria**: Daily EMA cross-under, fixed stop/breakeven, or max holding bars.
- **Stops**: Fixed stop with breakeven.
- **Default Values**:
  - `GapThreshold` = 1
  - `EpsSurpriseThreshold` = 5
  - `PerfDays` = 20
  - `StopPct` = 8
  - `EmaLen` = 50
  - `MaxHoldBars` = 50
  - `CandleType` = TimeSpan.FromDays(1)
- **Filters**:
  - Category: Earnings
  - Direction: Long
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
