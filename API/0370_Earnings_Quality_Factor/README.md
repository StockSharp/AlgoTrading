# Earnings Quality Factor
[Русский](README_ru.md) | [中文](README_zh.md)

The **Earnings Quality Factor** strategy rebalances annually on July 1, going long high quality and short low quality stocks based on earnings quality scores.

## Details
- **Entry Criteria**: Annual July 1 rebalance using quality scores.
- **Long/Short**: Both.
- **Exit Criteria**: Next annual rebalance.
- **Stops**: No.
- **Default Values**:
  - `MinTradeUsd = 100`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Fundamental
  - Direction: Both
  - Indicators: Quality
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
