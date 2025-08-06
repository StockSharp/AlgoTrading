# Residual Momentum Factor
[Русский](README_ru.md) | [中文](README_zh.md)

The **Residual Momentum Factor** strategy ranks securities by an external residual momentum score.
Each month on the first trading day it goes long the top decile and short the bottom decile.

## Details
- **Entry Criteria**: external residual momentum data feed.
- **Long/Short**: Both directions.
- **Exit Criteria**: Monthly rebalance.
- **Stops**: No explicit stop logic.
- **Default Values**:
  - `Decile = 10`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Fundamental
  - Direction: Both
  - Indicators: Fundamentals
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
