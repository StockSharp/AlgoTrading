# Bollinger Bands Heikin Ashi 入场
[English](README.md) | [Русский](README_ru.md)

该策略结合Heikin Ashi蜡烛与布林带。

当连续两到三根下跌Heikin Ashi蜡烛触及下轨后，若出现收盘价重新站上下轨的上涨蜡烛则做多；做空条件相反。达到第一目标价时平掉一半仓位，剩余部分使用跟踪止损。

## 详情
- **入场条件**: Bollinger Bands 附近的 Heikin Ashi 反转形态
- **多空方向**: 双向
- **退出条件**: 部分止盈与追踪止损
- **止损**: 是
- **默认值**:
  - `BollingerLength` = 20
  - `BollingerWidth` = 2
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类型: 反转
  - 方向: 双向
  - 指标: Heikin Ashi, Bollinger Bands
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (15m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

