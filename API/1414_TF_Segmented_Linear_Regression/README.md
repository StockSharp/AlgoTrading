# TF Segmented Linear Regression
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy applies a linear regression channel within each time segment. A long position opens when price crosses above the upper band and a short when it crosses below the lower band.

## Details
- **Entry Criteria**: Price crossing regression channel.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite band crossover.
- **Stops**: No.
- **Default Values**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `Segment` = TimeSpan.FromDays(1)
  - `Multiplier` = 2
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Linear Regression
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
