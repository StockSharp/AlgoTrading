# Turn of the Month Strategy Honestcowboy
[Русский](README_ru.md) | [中文](README_cn.md)

A calendar-based strategy that opens long positions near the end of each month and closes a few days into the new month, adjusting for weekends.

## Details

- **Entry Criteria**: dates near month end adjusted for weekends
- **Long/Short**: Long
- **Exit Criteria**: a few days after month start
- **Stops**: None
- **Default Values**:
  - `DaysBeforeEnd` = 2
  - `DaysAfterStart` = 3
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: None
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
