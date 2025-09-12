# Pavan CPR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Trades long when price crosses above the day's Top Central Pivot Range after previously closing below it. Stop is placed at the pivot level and take profit at a fixed distance.

## Details

- **Entry Criteria**: Previous close below top CPR and current close above it.
- **Long/Short**: Long only.
- **Exit Criteria**: Take profit or stop at pivot.
- **Stops**: Yes.
- **Default Values**:
  - `TakeProfitTarget` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Breakout
  - Direction: Long
  - Indicators: Pivot
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
