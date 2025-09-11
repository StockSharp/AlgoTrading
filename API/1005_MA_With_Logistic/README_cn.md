# MA With Logistic
[English](README.md) | [Русский](README_ru.md)

MA With Logistic 是一种基于移动平均线的策略，使用快慢均线判断入场，并支持百分比或逻辑概率的出场方式。

## 细节
- **数据**：价格K线。
- **入场条件**：
  - **多头**：收盘价 > 快速均线 且 快速均线 > 慢速均线。
  - **空头**：收盘价 < 快速均线 且 快速均线 < 慢速均线。
- **出场条件**：百分比目标或逻辑概率阈值。
- **止损**：百分比或逻辑概率。
- **默认值**：
  - `FastLength` = 9
  - `SlowLength` = 21
  - `MaType` = MaTypeEnum.EMA
  - `ExitType` = ExitTypeEnum.Percent
  - `TakeProfitPercent` = 20
  - `StopLossPercent` = 5
  - `LogisticSlope` = 10
  - `LogisticMidpoint` = 0
  - `TakeProfitProbability` = 0.8
  - `StopLossProbability` = 0.2
- **过滤器**：
  - 分类：趋势跟随
  - 方向：多头 & 空头
  - 指标：MA
  - 复杂度：低
  - 风险等级：中等
