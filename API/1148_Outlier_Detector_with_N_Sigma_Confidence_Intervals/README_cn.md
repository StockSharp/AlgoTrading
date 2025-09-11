# Outlier Detector with N-Sigma Confidence Intervals 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用 Nσ 置信区间识别价格变化中的异常波动，在出现极端运动时进行均值回归交易。

## 细节

- **入场条件**：
  - 当 z-score > `SecondLimit` 时做空。
  - 当 z-score < -`SecondLimit` 时做多。
- **多/空**：双向。
- **出场条件**：
  - 当 |z-score| < `FirstLimit` 时平仓。
- **止损**：无。
- **默认值**：
  - `SampleSize` = 30
  - `FirstLimit` = 2
  - `SecondLimit` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **筛选**：
  - 类别: Mean Reversion
  - 方向: 双向
  - 指标: StandardDeviation, Z-Score
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 风险等级: 中等
