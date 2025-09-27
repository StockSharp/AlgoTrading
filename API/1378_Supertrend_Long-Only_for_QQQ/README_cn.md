# Supertrend Long-Only for QQQ
[English](README.md) | [Русский](README_ru.md)

基于Supertrend指标并带日期过滤的QQQ多头策略。

## 详情
- **入场条件**: 价格上穿Supertrend。
- **多空方向**: 仅多头。
- **退出条件**: 价格下穿Supertrend。
- **止损**: 无。
- **默认值**:
  - `AtrPeriod` = 32
  - `Multiplier` = 4.35m
  - `StartDate` = 1995-01-01
  - `EndDate` = 2050-01-01
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**:
  - 类型: 趋势
  - 方向: 多头
  - 指标: ATR, Supertrend
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
