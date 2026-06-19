# The Z-Score 策略
[English](README.md) | [Русский](README_ru.md)

该策略计算基于 Heikin-Ashi 的 EMA 的 z-score，并根据近期范围的动态阈值交叉进行交易。

## 细节

- **入场条件**: score 突破最近低点或 EMA score 突破中线
- **多空方向**: 双向
- **出场条件**: EMA score 跌破最近高点或低点
- **止损**: 无
- **默认值**:
  - `HaEmaLength` = 10
  - `ScoreLength` = 25
  - `ScoreEmaLength` = 20
  - `RangeWindow` = 100
- **过滤器**:
  - 分类: 均值回归
  - 方向: 双向
  - 指标: EMA, SMA, StdDev, Highest, Lowest
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
