# VoVix DEVMA策略
[English](README.md) | [Русский](README_ru.md)

该策略基于ATR标准差构建的偏差移动平均线（DEVMA）来分析波动性变化。当快速DEVMA上穿或下穿慢速DEVMA时产生交易信号，并使用ATR倍数的止损和止盈。

## 详情

- **入场条件**：
  - **多头**：快速DEVMA上穿慢速DEVMA。
  - **空头**：快速DEVMA下穿慢速DEVMA。
- **多空方向**：双向。
- **退出条件**：
  - ATR止损和止盈。
- **止损**：是，ATR倍数。
- **默认值**：
  - `DeviationLookback` = 59
  - `FastLength` = 20
  - `SlowLength` = 60
  - `ATR SL Mult` = 2
  - `ATR TP Mult` = 3
- **过滤器**：
  - 类型：趋势跟随
  - 方向：双向
  - 指标：多个
  - 止损：是
  - 复杂度：复杂
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
