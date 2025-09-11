# Donky MA TP SL
[English](README.md) | [Русский](README_ru.md)

该策略利用均线交叉并设置两个止盈目标和一个止损。当快速SMA上穿慢速SMA时做多，下穿时做空。达到第一个目标时平掉一半仓位，其余在第二个目标或止损处平仓。

## 详情

- **入场条件**:
  - **多头**: 快速SMA上穿慢速SMA。
  - **空头**: 快速SMA下穿慢速SMA。
- **多空方向**: 双向。
- **离场条件**: 两个固定止盈位或固定止损。
- **止损**: 有。
- **默认值**:
  - `FastLength` = 10
  - `SlowLength` = 30
  - `TakeProfit1Pct` = 0.03m
  - `TakeProfit2Pct` = 0.06m
  - `StopLossPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: SMA
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 低
