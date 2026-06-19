# Tomas Ratio 多时间框分析策略
[English](README.md) | [Русский](README_ru.md)

该策略在多个时间框架上累积加权涨跌，构建 Tomas Ratio 信号。当信号强度超过目标且价格位于 EMA(720) 之上时开多仓，当弱势占主导时平仓。

## 详情

- **入场条件**：信号强度超过目标且价格高于 EMA(720)
- **多空方向**：仅做多
- **出场条件**：平仓点数大于买入点数
- **止损**：无
- **默认值**：
  - `CandleType` = 1 小时蜡烛
  - `Length` = 720
  - `DeviationLength` = 168
  - `PointsTarget` = 100
  - `UseStandardDeviation` = true
- **筛选**：
  - 类别: 动量
  - 方向: 多头
  - 指标: Standard Deviation, SMA, EMA
  - 止损: 无
  - 复杂度: 高级
  - 时间框架: 多时间框
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 高
