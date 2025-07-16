# 布林回归
[English](README.md) | [Русский](README_ru.md)

该策略在价格收于布林带外时逆势入场，待价格回到带内或触及止损后离场。布林带作为标准差通道揭示过度延伸，期望利用价格回归中轨的过程获利。

## 详情
- **入场条件**: 基于 RSI、ATR、Bollinger 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 均值回归
  - 方向: 双向
  - 指标: RSI, ATR, Bollinger
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
