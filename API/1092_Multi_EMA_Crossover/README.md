# Multi EMA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy opens separate long positions for four EMA pairs when the faster EMA crosses above the slower one. Each position closes when its faster EMA falls below the slower EMA.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA crosses above slow EMA for any of the pairs (1/5, 3/10, 5/20, 10/40).
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Fast EMA drops below slow EMA for the respective pair.
- **Stops**: None.
- **Default Values**:
  - `EMA1` = 1
  - `EMA3` = 3
  - `EMA5` = 5
  - `EMA10` = 10
  - `EMA20` = 20
  - `EMA40` = 40
- **Filters**:
  - Category: Trend following
  - Direction: Long
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Low
