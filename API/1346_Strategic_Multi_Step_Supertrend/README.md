# Strategic Multi Step Supertrend
[Русский](README_ru.md) | [中文](README_cn.md)

Strategy uses two Supertrend calculations to detect entries and exits with configurable multi-step take profits.

## Details

- **Entry Criteria**: Signals based on two Supertrend directions.
- **Long/Short**: Configurable.
- **Exit Criteria**: Opposite Supertrend or take profit levels.
- **Stops**: Take profit steps.
- **Default Values**:
  - `UseTakeProfit` = true
  - `TakeProfitPercent1` = 6.0
  - `TakeProfitPercent2` = 12.0
  - `TakeProfitPercent3` = 18.0
  - `TakeProfitPercent4` = 50.0
  - `TakeProfitAmount1` = 12
  - `TakeProfitAmount2` = 8
  - `TakeProfitAmount3` = 4
  - `TakeProfitAmount4` = 0
  - `NumberOfSteps` = 3
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 5
  - `Factor2` = 4.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Trend
  - Direction: Configurable
  - Indicators: ATR, Supertrend
  - Stops: Take profit
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
