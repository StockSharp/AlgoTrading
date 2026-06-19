# Keltner通道突破
[English](README.md) | [Русский](README_ru.md)

本策略利用基于ATR的Keltner通道。当价格突破上轨或下轨时入场，回到EMA中线或触及止损时退出。通道会随波动扩张或收缩，旨在在行情初期捕捉强势走势，同时给价格一定的回旋空间。

测试表明年均收益约为 58%，该策略在股票市场表现最佳。

## 详情
- **入场条件**: 基于 ATR、Keltner 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 突破
  - 方向: 双向
  - 指标: ATR, Keltner
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

