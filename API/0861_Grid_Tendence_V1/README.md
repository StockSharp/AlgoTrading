# Grid Tendence V1 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

Grid trading strategy that reopens or reverses positions based on profit percentage steps.

It starts long and when profit reaches the specified percent it closes and reopens in the same direction. When loss reaches the percent it closes and opens in the opposite direction.

## Details

- **Entry Criteria**: Always in market, starting long. Reopen or reverse when profit or loss reaches `Percent`.
- **Long/Short**: Both directions.
- **Exit Criteria**: Profit or loss threshold.
- **Stops**: No.
- **Default Values**:
  - `Percent` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filters**:
  - Category: Grid
  - Direction: Both
  - Indicators: None
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday (1m)
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium
