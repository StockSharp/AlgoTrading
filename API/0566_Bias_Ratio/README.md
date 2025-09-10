# Bias Ratio Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The Bias Ratio strategy trades breakouts based on price deviation from long-term moving averages. It compares the close price to both an exponential moving average (EMA) and a simple moving average (SMA). A long position is opened when price exceeds the EMA by a specified ratio, while a short position is opened when price falls below the SMA by the same ratio.

## Details

- **Entry Criteria**:
  - `close / EMA >= 1 + BiasThreshold` → enter long.
  - `close / SMA <= 1 - BiasThreshold` → enter short.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Opposite signal closes and reverses positions.
- **Stops**: None.
- **Default Values**:
  - `MaPeriod` = 200
  - `BiasThreshold` = 0.025
- **Filters**:
  - Category: Trend following
  - Direction: Long & Short
  - Indicators: EMA, SMA
  - Stops: No
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
