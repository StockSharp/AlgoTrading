# Weighted Ichimoku Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy combines Ichimoku signals into a weighted score.
It buys when the score exceeds the buy threshold and exits when the score drops below the sell threshold.

## Details

- **Entry Criteria**: score >= BuyThreshold
- **Long/Short**: Long only
- **Exit Criteria**: score <= SellThreshold or below zero if threshold disabled
- **Stops**: No
- **Default Values**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `Offset` = 26
  - `BuyThreshold` = 60
  - `SellThreshold` = -49
- **Filters**:
  - Category: Trend
  - Direction: Long
  - Indicators: Ichimoku
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

