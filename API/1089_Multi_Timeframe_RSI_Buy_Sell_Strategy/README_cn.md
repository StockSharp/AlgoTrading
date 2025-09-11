# 多时间框架RSI买卖策略
[English](README.md) | [Русский](README_ru.md)

该策略使用三个不同时间框架的RSI值。当所有启用的RSI低于买入阈值时开多仓；当所有启用的RSI高于卖出阈值时开空仓。冷却期可以避免连续信号。

## 细节

- **入场条件**：所有启用的RSI低于/高于阈值。
- **多空方向**：双向。
- **退出条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `Rsi1Length` = 14
  - `Rsi2Length` = 14
  - `Rsi3Length` = 14
  - `Rsi1CandleType` = TimeSpan.FromMinutes(5)
  - `Rsi2CandleType` = TimeSpan.FromMinutes(15)
  - `Rsi3CandleType` = TimeSpan.FromMinutes(30)
  - `BuyThreshold` = 30m
  - `SellThreshold` = 70m
  - `CooldownPeriod` = 5
- **过滤器**：
  - 类别: Momentum
  - 方向: 双向
  - 指标: RSI
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 多时间框架
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
