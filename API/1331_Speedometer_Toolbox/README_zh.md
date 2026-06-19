# Speedometer Toolbox
[English](README.md) | [Русский](README_ru.md)

该策略仅用于可视化，基于 RSI 绘制类似速度计的仪表。

## 详情

- **入场条件**: 无，仅可视化。
- **多空方向**: 无。
- **退出条件**: 不适用。
- **止损**: 无。
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
  - `RsiLength` = 14
  - `Radius` = 20
- **过滤器**:
  - 类型: 可视化
  - 方向: 无
  - 指标: RSI
  - 止损: 否
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 无
