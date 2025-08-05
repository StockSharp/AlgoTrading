# Commodity Momentum
[Русский](README_ru.md) | [中文](README_zh.md)

The **Commodity Momentum** strategy longs commodities with the strongest 12-month momentum (skipping the most recent month).
Positions are rebalanced on the first trading day of each month.

Testing indicates an average annual return of about 10%. It performs best across diversified commodity markets.

Positions are adjusted monthly; no intraday signals are used.

## Details
- **Entry Criteria**: Buy top `TopN` commodities by 12-month momentum excluding last month.
- **Long/Short**: Long only.
- **Exit Criteria**: Rebalance on the next scheduled date.
- **Stops**: No explicit stop logic.
- **Default Values**:
  - `TopN = 5`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Price
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
