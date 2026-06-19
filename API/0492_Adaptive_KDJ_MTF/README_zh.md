# 自适应KDJ (MTF)
[English](README.md) | [Русский](README_ru.md)

自适应KDJ策略将三个不同时间框架的KDJ振荡器值进行加权平均，并使用EMA进行平滑处理。合成振荡器的SMA用来衡量趋势强度，从而动态调整超买和超卖水平。

当J线位于自适应买入水平下方且K线向上穿越D线时，策略做多。J线高于自适应卖出水平且K线向下穿越D线时，策略做空。

## 详情

- **入场条件**: KDJ交叉且J线低于/高于动态水平。
- **多空方向**: 双向。
- **出场条件**: 反向信号。
- **止损**: 无。
- **默认值**:
  - `TimeFrame1` = TimeSpan.FromMinutes(1)
  - `TimeFrame2` = TimeSpan.FromMinutes(3)
  - `TimeFrame3` = TimeSpan.FromMinutes(15)
  - `KdjLength` = 9
  - `SmoothingLength` = 5
  - `TrendLength` = 40
  - `WeightOption` = 1
- **过滤器**:
  - 类别: Oscillator
  - 方向: 双向
  - 指标: Stochastic, EMA, SMA
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
