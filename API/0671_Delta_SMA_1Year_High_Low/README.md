# Delta SMA 1-Year High Low
[Русский](README_ru.md) | [中文](README_cn.md)

The **Delta SMA 1-Year High Low** strategy calculates volume delta (buy minus sell volume) and its simple moving average. It goes long when the delta SMA was very low and then crosses above zero. The position is closed when the delta SMA drops below 60% of its 1-year high after previously crossing above 70% of that high.

## Details
- **Entry Criteria**: Delta SMA was below 70% of its 1-year low and crosses above zero.
- **Long/Short**: Long only.
- **Exit Criteria**: Delta SMA falls below 60% of its 1-year high after crossing 70%.
- **Stops**: No.
- **Default Values**:
  - `DeltaSmaLength = 14`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **Filters**:
  - Category: Volume
  - Direction: Long
  - Indicators: SMA, Highest, Lowest
  - Stops: No
  - Complexity: Intermediate
  - Timeframe: Daily
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
