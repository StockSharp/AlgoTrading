# Gold Volume-Based Entry Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy buys when two consecutive bullish volume bars exceed the volume moving average. The second bar must also have higher volume than the first. A fixed profit target closes the position once price moves a predefined amount in favor.

## Details

- **Entry Criteria**:
  - Two bullish volume bars above the volume moving average with rising volume.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Fixed profit target at `entry price + Target Move`.
- **Stops**: None.
- **Default Values**:
  - `Volume MA Period` = 20.
  - `Target Move` = 5.
- **Filters**:
  - Category: Volume
  - Direction: Long
  - Indicators: Single
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
