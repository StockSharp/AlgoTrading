# Universum 3.0 策略
[English](README.md) | [Русский](README_ru.md)

基于 DeMarker 振荡指标的策略，在每根完成的K线开仓，并使用马丁格尔方式调整仓位。

## 详情

- **入场条件**:
  - 多头: `DeMarker > 0.5`
  - 空头: `DeMarker < 0.5`
- **方向**: 多头和空头
- **出场条件**:
  - 通过止盈或止损平仓
- **止损**: 通过 `TakeProfitPoints` 和 `StopLossPoints` 指定绝对点数
- **默认值**:
  - `DemarkerPeriod` = 10
  - `TakeProfitPoints` = 50m
  - `StopLossPoints` = 50m
  - `InitialVolume` = 1m
  - `LossesLimit` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**:
  - 分类: 趋势跟随
  - 方向: 多头和空头
  - 指标: DeMarker
  - 止损: 是
  - 复杂度: 低
  - 时间框架: 短期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 高
