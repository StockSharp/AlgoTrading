# Skewness Commodity
[Русский](README_ru.md) | [中文](README_zh.md)

The **Skewness Commodity** strategy ranks commodity futures by the skewness of their return distribution. Contracts with positive skewness are favored for long positions, while those with strongly negative skewness are sold short, assuming extreme downside moves will mean revert.

## Details
- **Entry Criteria**: Ranking by historical return skewness.
- **Long/Short**: Both directions.
- **Exit Criteria**: Periodic rebalance.
- **Stops**: No explicit stop.
- **Default Values**:
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Statistical
  - Direction: Both
  - Indicators: Price based
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Medium-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
