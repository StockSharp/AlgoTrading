# Laguerre RSI
[English](README.md) | [Русский](README_ru.md)

该策略使用平滑的Laguerre RSI，减少传统RSI的噪音。当Laguerre值从超卖区向上穿越时买入，从超买区向下穿越时卖出，回到中值附近离场。平滑处理可避免普通RSI在震荡市频繁给出假信号，适合捕捉日内波段。

## 详情
- **入场条件**: 基于 RSI 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `Gamma` = 0.7m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: RSI
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
