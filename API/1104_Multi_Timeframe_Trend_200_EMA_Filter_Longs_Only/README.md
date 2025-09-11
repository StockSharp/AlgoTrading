# Multi-Timeframe Trend Following with 200 EMA Filter - Longs Only
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy goes long when the fast EMA is above the slow EMA on 5, 15 and 30 minute charts and the price is above the 200 EMA on the 5 minute chart. The position is closed if any timeframe turns bearish or the price drops below the 200 EMA.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA > Slow EMA on 5, 15 and 30 minute timeframes and close > 200 EMA (5m).
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Any timeframe trend turns negative or close < 200 EMA (5m).
- **Stops**:
  - Stop Loss: percentage.
  - Take Profit: percentage.
- **Default Values**:
  - `Fast EMA Length` = 9
  - `Slow EMA Length` = 21
  - `200 EMA Length` = 200
  - `Stop Loss %` = 1
  - `Take Profit %` = 3
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: 5m base with 15m and 30m confirmation
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
