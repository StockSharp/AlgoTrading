# Yeong RRG
[English](README.md) | [Русский](README_ru.md)

基于归一化相对强度和动量比率（RRG）的策略。

当 JDK RS 和 JDK RoC 同时高于 100 时做多，二者同时低于 100 时退出。

## 详情
- **入场条件**: JDK RS 和 JDK RoC 高于 100
- **多空方向**: 仅多头
- **退出条件**: JDK RS 和 JDK RoC 低于 100
- **止损**: 无
- **默认值**:
  - `Length` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: Relative Strength
  - 方向: Long
  - 指标: SMA, ROC, StandardDeviation
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中

