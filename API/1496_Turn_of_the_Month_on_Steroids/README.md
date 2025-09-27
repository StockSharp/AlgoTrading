# Turn of the Month Strategy on Steroids
[Русский](README_ru.md) | [中文](README_cn.md)

A seasonal strategy that buys near the end of each month after two consecutive down closes and exits when a short RSI signals overbought conditions.

## Details

- **Entry Criteria**: day of month above threshold and two-day decline
- **Long/Short**: Long
- **Exit Criteria**: RSI above threshold
- **Stops**: None
- **Default Values**:
  - `DayOfMonth` = 25
  - `RsiLength` = 2
  - `RsiThreshold` = 65
- **Filters**:
  - Category: Seasonality
  - Direction: Long
  - Indicators: RSI
  - Stops: No
  - Complexity: Beginner
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
