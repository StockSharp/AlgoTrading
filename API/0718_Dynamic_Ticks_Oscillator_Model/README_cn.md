# 动态 Tick 振荡模型 (DTOM)
[English](README.md) | [Русский](README_ru.md)

**Dynamic Ticks Oscillator Model** 监控 NYSE Down Ticks 指数的变动率。当 ROC 跌破基于标准差的动态阈值时策略做多；当 ROC 突破正阈值时平仓。

## 详细信息
- **入场条件**: `ROC < -StdDev * EntryStdDevMultiplier`
- **多空**: 仅做多。
- **出场条件**: `ROC > StdDev * ExitStdDevMultiplier`
- **止损**: 无。
- **默认值**:
  - `RocLength = 5`
  - `VolatilityLookback = 24`
  - `EntryStdDevMultiplier = 1.6m`
  - `ExitStdDevMultiplier = 1.4m`
  - `CandleType = TimeSpan.FromDays(1).TimeFrame()`
- **过滤器**:
  - 分类: 均值回归
  - 方向: 多头
  - 指标: RateOfChange, StandardDeviation
  - 止损: 无
  - 复杂度: 初级
  - 时间框架: 日线
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险级别: 中等

