# Cci Threshold Strategy
[English](README.md) | [Русский](README_ru.md)

基于 CCI 阈值的简单策略。当 CCI 低于阈值时买入，当收盘价高于上一根收盘价时平仓。
可选的止损和止盈（点数）。

## 详情

- **入场条件**:
  - 多头: `CCI < BuyThreshold`
- **多/空**: 仅多头
- **离场条件**:
  - `ClosePrice > 上一根收盘价`
- **止损**: 可通过 `UseStopLoss` 和 `UseTakeProfit`
- **默认值**:
  - `LookbackPeriod` = 12
  - `BuyThreshold` = -90
  - `StopLossPoints` = 100m
  - `TakeProfitPoints` = 150m
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **筛选**:
  - 类别: 均值回归
  - 方向: 多头
  - 指标: CCI
  - 止损: 可选
  - 复杂度: 低
  - 时间框架: 短期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
