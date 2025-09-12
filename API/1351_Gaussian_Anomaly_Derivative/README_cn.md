# Gaussian Anomaly Derivative 策略
[English](README.md) | [Русский](README_ru.md)

使用价格异常值 `1 - (high + low) / (2 * close)` 的移动平均及其平滑导数。
当导数高于正阈值时做多，低于负阈值时做空。

## 细节

- **入场条件**: 异常值或其导数越过阈值
- **多空方向**: 可配置
- **出场条件**: 相反信号
- **止损**: 无
- **默认值**:
  - `UseSma` = true
  - `MaPeriod` = 3
  - `DerivativeMaPeriod` = 2
  - `ThresholdCoeff` = 1.0
  - `DerivativeThresholdCoeff` = 1.0
  - `StartBarCount` = 600
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: SMA, EMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
