# Iron Bot Statistical Trend Filter Strategy
[English](README.md) | [Русский](README_ru.md)

该策略利用斐波那契区间和Z分数计算的统计趋势水平进行突破交易。

## 详情

- **入场条件**:
  - **多头**: 价格突破趋势线和高位水平，Z分数≥0。
  - **空头**: 价格跌破趋势线和低位水平，Z分数≤0。
- **多空**: 双向。
- **出场条件**:
  - 止损：距离开仓价 `SlRatio` 百分比。
  - 止盈：在四个水平之一 (`Tp1Ratio`–`Tp4Ratio`)。
- **Stops**: 是。
- **默认值**:
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
