# External Signals Strategy Tester
[English](README.md) | [Русский](README_ru.md)

该模板策略根据外部多空信号上穿零值执行交易。支持可选的反向开仓、在反向信号时平仓，以及百分比止盈、止损和保本。

## 细节

- **入场条件**：在指定日期范围内，多头或空头信号序列上穿零值。
- **多空方向**：双向。
- **出场条件**：止盈、止损或保本止损。
- **止损**：有。
- **默认值**：
  - `StartDate` = 2024-11-01 00:00:00
  - `EndDate` = 2025-03-31 23:59:00
  - `EnableLong` = true
  - `EnableShort` = true
  - `CloseOnReverse` = true
  - `ReversePosition` = false
  - `TakeProfitPerc` = 2
  - `StopLossPerc` = 1
  - `BreakevenPerc` = 1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别: Signals
  - 方向: 双向
  - 指标: 无
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中
