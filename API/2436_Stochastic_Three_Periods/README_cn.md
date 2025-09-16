# Stochastic Three Periods
[English](README.md) | [Русский](README_ru.md)

**Stochastic Three Periods** 策略在三个时间框架上对齐随机指标信号。 当快速随机指标发生交叉且两个更高时间框架同向时开仓。

## 细节

- **入场条件**：快速 %K 向上/下穿越 %D，且在 `ShiftEntrance` 根K线前出现相反关系；两个更高时间框架的 %K 均在 %D 之上或之下；收盘价配合信号方向。
- **多空方向**：双向。
- **出场条件**：上一根K线的快速随机指标出现反向交叉。
- **止损止盈**：通过 `StartProtection` 以点数固定。
- **默认值**：
  - `CandleType1` = 5m
  - `CandleType2` = 15m
  - `CandleType3` = 30m
  - `KPeriod1` = 5
  - `KPeriod2` = 5
  - `KPeriod3` = 5
  - `KExitPeriod` = 5
  - `ShiftEntrance` = 3
  - `TakeProfitPoints` = 30
  - `StopLossPoints` = 10
- **筛选**：
  - 类型: 振荡指标
  - 方向: 双向
  - 指标: Stochastic
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
