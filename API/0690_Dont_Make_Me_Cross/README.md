# Dont Make Me Cross
[Русский](README_ru.md) | [中文](README_cn.md)

EMA crossover strategy with vertical shift.

## Details

- **Entry Criteria**:
  - **Long**: Shifted short EMA crosses above shifted long EMA.
  - **Short**: Shifted short EMA crosses below shifted long EMA.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover.
- **Stops**: No.
- **Default Values**:
  - `ShortEmaLength` = 9
  - `LongEmaLength` = 21
  - `ShiftAmount` = -50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
