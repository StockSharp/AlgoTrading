# 价格无线电策略
[English](README.md) | [Русский](README_ru.md)

该策略实现了 John Ehlers 的 Price Radio 指标。当价格导数同时超过振幅和频率阈值时做多，跌破其负值时做空。

## 细节

- **入场条件**：
  - **多头**：导数大于振幅和频率。
  - **空头**：导数小于负振幅且小于负频率。
- **多空**：双向。
- **离场条件**：相反信号。
- **止损**：无。
- **默认参数**：
  - `Length` = 14。
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()。
- **过滤器**：
  - Category: Oscillator
  - Direction: Both
  - Indicators: Custom
  - Stops: No
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
