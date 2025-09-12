# Long-Only MTF EMA Cloud
[Русский](README_ru.md) | [中文](README_cn.md)

EMA cloud crossover strategy that trades long when the short EMA crosses above the long EMA. Uses fixed percentage stop loss and take profit.

## Details

- **Entry Criteria**: Short EMA crosses above long EMA.
- **Long/Short**: Long only.
- **Exit Criteria**: Price hits stop loss or take profit.
- **Stops**: Fixed percentage stop loss and take profit.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(1)
  - `ShortLength` = 21
  - `LongLength` = 50
  - `StopLossPercent` = 1.0m
  - `TakeProfitPercent` = 2.0m
- **Filters**:
  - Category: Trend-following
  - Direction: Long
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Beginner
  - Timeframe: Any
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
