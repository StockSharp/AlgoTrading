# Supertrend Fixed Tp Unified With Time Filter Msk Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy based on Supertrend indicator with fixed percentage take profit, optional price filter and time filter in Moscow timezone.

## Details
- **Entry Criteria**: Supertrend direction change with optional price and time filters
- **Long/Short**: Configurable (long, short or both)
- **Exit Criteria**: Fixed take profit or opposite signal
- **Stops**: Take profit only
- **Default Values**:
  - `AtrPeriod` = 23
  - `Factor` = 1.8m
  - `TakeProfitPercent` = 1.5m
  - `PriceFilter` = 10000m
  - `TimeFrom` = 0
  - `TimeTo` = 23
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Supertrend
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
