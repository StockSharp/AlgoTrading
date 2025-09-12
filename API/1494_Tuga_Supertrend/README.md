# Tuga Supertrend
[Русский](README_ru.md) | [中文](README_cn.md)

Tuga Supertrend is a long-only strategy based on the SuperTrend indicator. It enters a long position when the SuperTrend direction flips downward and exits when the direction turns upward.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: SuperTrend direction changes from up to down within the date window.
- **Exit Criteria**: SuperTrend direction changes from down to up.
- **Stops**: None.
- **Default Values**:
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `AtrPeriod` = 10
  - `Factor` = 3.0
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: SuperTrend, ATR
  - Complexity: Low
  - Risk level: Medium
