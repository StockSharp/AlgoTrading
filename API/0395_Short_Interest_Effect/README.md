# Short Interest Effect
[Русский](README_ru.md) | [中文](README_zh.md)

The **Short Interest Effect** strategy uses short interest levels to predict stock performance. Securities with low days-to-cover tend to outperform those heavily shorted. At a monthly interval, stocks are sorted by short interest and the portfolio buys the lowest group while shorting the highest.

## Details
- **Entry Criteria**: Monthly ranking by short interest ratio or days-to-cover.
- **Long/Short**: Both directions.
- **Exit Criteria**: Monthly rebalance.
- **Stops**: No explicit stop.
- **Default Values**:
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Fundamental
  - Direction: Both
  - Indicators: Fundamentals
  - Stops: No
  - Complexity: Basic
  - Timeframe: Medium-term
  - Seasonality: Yes
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
