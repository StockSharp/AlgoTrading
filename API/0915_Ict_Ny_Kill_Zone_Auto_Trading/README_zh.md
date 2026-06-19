# ICT NY Kill Zone Auto Trading
[English](README.md) | [Русский](README_ru.md)

该策略在纽约杀戮时段内利用公平价值缺口和订单块进行交易。

## 详情

- **入场条件**: Kill zone 内的公平价值缺口和订单块。
- **多空方向**: 双向。
- **退出条件**: 位置保护。
- **止损**: 是。
- **默认值**:
  - `StopLoss` = 30
  - `TakeProfit` = 60
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: Breakout
  - 方向: 双向
  - 指标: Price Action
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

