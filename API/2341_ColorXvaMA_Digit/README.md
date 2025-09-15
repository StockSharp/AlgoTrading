# ColorXvaMA Digit Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the slope change of a double-smoothed moving average. An Exponential Moving Average is smoothed again by a Jurik Moving Average. A long position opens when the fast JMA crosses above the slow EMA, while a short position opens when it crosses below.

## Details

- **Entry Criteria**:
  - **Long**: Fast JMA crosses above slow EMA.
  - **Short**: Fast JMA crosses below slow EMA.
- **Long/Short**: Both sides.
- **Exit Criteria**: Opposite signal.
- **Stops**: None.
- **Default Values**:
  - `SlowLength` = 15
  - `FastLength` = 5
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: EMA, JMA
  - Stops: None
  - Complexity: Low
  - Timeframe: 8h
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
