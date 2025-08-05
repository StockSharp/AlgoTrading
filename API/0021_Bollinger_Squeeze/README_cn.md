# 布林收窄突破
[English](README.md) | [Русский](README_ru.md)

该策略关注布林带极窄时的低波动，一旦价格突破带宽就顺势入场，动量衰竭或出现反向突破时离场。带宽收窄预示即将爆发的波动，进入后依靠ATR止损或带线交叉退出。

测试表明年均收益约为 100%，该策略在外汇市场表现最佳。

## 详情
- **入场条件**: 基于 Bollinger 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `SqueezeThreshold` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: Bollinger
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

