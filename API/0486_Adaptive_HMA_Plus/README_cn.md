# Adaptive HMA Plus
[English](README.md) | [Русский](README_ru.md)

基于自适应 Hull 均线的策略，根据波动率或成交量调整周期。当市场活跃且 HMA 斜率指向趋势方向时开多或做空。

## 详情

- **入场条件**: 基于自适应 HMA、ATR 或成交量的信号
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `MinPeriod` = 172
  - `MaxPeriod` = 233
  - `AdaptPercent` = 0.031m
  - `FlatThreshold` = 0m
  - `UseVolume` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: MA, ATR, Volume
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

