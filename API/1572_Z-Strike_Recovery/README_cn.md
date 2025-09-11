# Z-Strike Recovery 策略
[English](README.md) | [Русский](README_ru.md)

当价格变化的 z-score 超过阈值时做多，并在固定的柱数后平仓。

## 细节

- **入场条件**: 价格变化的 z-score > 阈值
- **多空**: 仅多头
- **出场条件**: 时间退出
- **止损**: 无
- **默认值**:
  - `ZLength` = 16
  - `ZThreshold` = 1.3
  - `ExitPeriods` = 10
- **筛选**:
  - 类别: 统计
  - 方向: 多头
  - 指标: SMA, StandardDeviation
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
