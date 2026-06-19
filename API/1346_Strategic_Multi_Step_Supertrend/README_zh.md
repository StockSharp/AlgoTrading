# Strategic Multi Step Supertrend
[English](README.md) | [Русский](README_ru.md)

该策略使用两个 Supertrend 指标并带有多级分批止盈。

## 细节

- **入场条件**：基于两个 Supertrend 方向的信号。
- **多空**：可配置。
- **出场条件**：相反的 Supertrend 信号或止盈等级。
- **止损**：分批止盈。
- **默认值**：
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
- **筛选器**：
  - 类别: 趋势
  - 方向: 可配置
  - 指标: ATR, Supertrend
  - 止损: 止盈
  - 复杂度: 中等
  - 周期: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
