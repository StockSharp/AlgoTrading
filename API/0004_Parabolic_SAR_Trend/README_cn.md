# 抛物线SAR趋势策略
[English](README.md) | [Русский](README_ru.md)

本策略依据抛物线SAR指标。当价格从SAR的一侧翻转到另一侧，意味着可能的趋势变化，若价格再次穿越则平仓。由于SAR点位跟随价格，其本身就提供了离场位，因此策略做多做空均无需额外止损。

## 详情
- **入场条件**: 根据 Parabolic SAR 信号
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `AccelerationFactor` = 0.02m
  - `MaxAccelerationFactor` = 0.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: Parabolic SAR
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
