# Balance of Power Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Balance of Power strategy evaluates the strength of bulls versus bears within each candle by comparing the close to the trading range. When this value crosses above a positive threshold, it indicates strong buying pressure.

The strategy enters a long position when Balance of Power crosses above the defined `Threshold` and exits when it drops below the negative threshold.

## Details

- **Entry Criteria**:
  - Balance of Power crosses above `Threshold`.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Balance of Power crosses below `-Threshold`.
- **Stops**: None.
- **Default Values**:
  - `Threshold` = 0.8
- **Filters**:
  - Category: Momentum
  - Direction: Long
  - Indicators: Balance of Power
  - Stops: None
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
