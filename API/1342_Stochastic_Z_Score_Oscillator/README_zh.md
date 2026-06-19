# Stochastic Z-Score Oscillator 策略
[English](README.md) | [Русский](README_ru.md)

将缩放后的随机指标与价格 Z-Score 结合。当两者的平均值越过阈值时开仓，当 Z-Score 返回零值时平仓。冷却计数器可防止频繁信号。

## 细节

- **入场条件**: 缩放随机指标与 Z-Score 的平均值在冷却期后越过阈值
- **多空方向**: 双向
- **出场条件**: Z-Score 穿越零轴
- **止损**: 无
- **默认值**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `StochLength` = 14
  - `StochSmooth` = 7
- **过滤器**:
  - 分类: 振荡指标
  - 方向: 双向
  - 指标: Stochastic, SMA, StandardDeviation
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 任意
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
