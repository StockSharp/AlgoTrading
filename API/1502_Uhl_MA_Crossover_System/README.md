# Uhl MA Crossover System
[Русский](README_ru.md) | [中文](README_cn.md)

The Uhl MA Crossover System builds two adaptive lines (CTS and CMA) using variance to adjust smoothing. A long position is opened when CTS crosses above CMA and a short when it crosses below.

## Details

- **Entry Criteria**: CTS crosses above CMA.
- **Long/Short**: Both.
- **Exit Criteria**: CTS crosses below CMA.
- **Stops**: No.
- **Default Values**:
  - `Length` = 100
  - `Multiplier` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: SMA, Variance
  - Stops: No
  - Complexity: Basic
  - Timeframe: Short-term
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
