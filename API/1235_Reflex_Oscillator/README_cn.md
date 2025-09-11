# Reflex Oscillator 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 John Ehlers 的 Reflex 振荡器。当振荡器上穿上阈值时做多，下穿下阈值时做空。当振荡器返回到零线时平仓。

## 详情

- **入场条件**:
  - **多头**: 振荡器上穿 `UpperLevel`。
  - **空头**: 振荡器下穿 `LowerLevel`。
- **多空方向**: 双向。
- **出场条件**:
  - 多头: 振荡器下穿零线。
  - 空头: 振荡器上穿零线。
- **止损**: 无。
- **默认值**:
  - `ReflexPeriod` = 20。
  - `SuperSmootherPeriod` = 8。
  - `PostSmoothPeriod` = 33。
  - `UpperLevel` = 1。
  - `LowerLevel` = -1。
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **过滤器**:
  - 类型: 振荡器
  - 方向: 双向
  - 指标: 单一
  - 止损: 无
  - 复杂度: 高级
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
