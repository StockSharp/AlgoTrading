# Iron Bot Statistical Trend Filter Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The strategy trades breakouts based on statistical trend levels computed from Fibonacci ranges and Z-score.

## Details

- **Entry Criteria**:
  - **Long**: price crosses above trend line and high trend level with non-negative Z-score.
  - **Short**: price crosses below trend line and low trend level with non-positive Z-score.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - Stop loss at `SlRatio` percent from entry.
  - Take profit at one of four levels (`Tp1Ratio`–`Tp4Ratio`) from entry.
- **Stops**: Yes.
- **Default Values**:
  - `ZLength` = 40.
  - `AnalysisWindow` = 44.
  - `HighTrendLimit` = 0.236.
  - `LowTrendLimit` = 0.786.
  - `EmaLength` = 200.
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: Z-score, EMA, price action
  - Stops: Yes
  - Complexity: Intermediate
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
