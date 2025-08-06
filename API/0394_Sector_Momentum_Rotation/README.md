# Sector Momentum Rotation
[Русский](README_ru.md) | [中文](README_zh.md)

The **Sector Momentum Rotation** strategy rotates capital among sector ETFs. At the end of each month the trailing return of each sector over several lookback windows is calculated. The system buys the strongest sectors and exits weaker ones, maintaining exposure only to top performers.

## Details
- **Entry Criteria**: Monthly ranking of sector ETF momentum.
- **Long/Short**: Long only.
- **Exit Criteria**: Rebalanced monthly when rankings change.
- **Stops**: No explicit stop.
- **Default Values**:
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Price based
  - Stops: No
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
