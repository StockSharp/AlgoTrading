# Supertrend 5m
[English](README.md) | [Русский](README_ru.md)

基于5分钟K线的Supertrend策略。

## 详情
- **入场条件**: 价格上穿Supertrend。
- **多空方向**: 仅多头。
- **退出条件**: 价格下穿Supertrend。
- **止损**: 无。
- **默认值**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 多头
  - 指标: ATR, Supertrend
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
