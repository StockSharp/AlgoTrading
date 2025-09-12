# HMA Crossover RSI Stochastic Trailing Stop
[English](README.md) | [Русский](README_ru.md)

基于快速与慢速 HMA 交叉，并结合 RSI 与平滑 Stochastic 过滤的策略。当快速 HMA 上穿慢速且 RSI 和 Stochastic 低于阈值时开多，反向条件开空。通过百分比追踪止损退出。

## 详情

- **入场条件**: 快速 HMA 上穿慢速且 RSI、Stochastic 低于阈值。
- **多空方向**: 双向。
- **退出条件**: 追踪止损或反向信号。
- **止损**: 百分比追踪。
- **默认值**:
  - `FastHmaLength` = 5
  - `SlowHmaLength` = 20
  - `RsiPeriod` = 14
  - `RsiBuyLevel` = 45
  - `RsiSellLevel` = 60
  - `StochLength` = 14
  - `StochSmooth` = 3
  - `StochBuyLevel` = 39
  - `StochSellLevel` = 63
  - `TrailingPercent` = 5
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: HMA, RSI, Stochastic
  - 止损: 追踪
  - 复杂度: 基础
  - 时间框架: 1h
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
