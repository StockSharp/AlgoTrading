# Molly ETF EMA Crossover Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy enters a long position when the fast EMA crosses above the slow EMA and exits when the fast EMA crosses below. It includes optional parameters to restrict trading to a specific date range.

## Details

- **Entry Criteria**:
  - **Long**: Fast EMA crosses above slow EMA within the date range.
- **Long/Short**: Long only.
- **Exit Criteria**:
  - Fast EMA crosses below slow EMA or the date range ends.
- **Stops**: None.
- **Default Values**:
  - `Fast EMA` = 10
  - `Slow EMA` = 21
  - `Start Date` = 2018-01-01
  - `End Date` = 2023-09-07
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
