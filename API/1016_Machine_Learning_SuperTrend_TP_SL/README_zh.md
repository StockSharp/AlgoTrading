# 机器学习 SuperTrend 止盈止损
[English](README.md) | [Русский](README_ru.md)

基于 SuperTrend 指标，并设置跟踪止盈与止损的策略。

止盈与止损随 SuperTrend 线移动，尝试抓住趋势同时在动能减弱时锁定利润。

## 详情
- **入场条件**: 价格穿越 SuperTrend 线。
- **多空方向**: 双向。
- **退出条件**: 相反信号或触发跟踪止盈/止损。
- **止损**: 是，依据 SuperTrend 跟踪。
- **默认值**:
  - `AtrPeriod` = 4
  - `AtrFactor` = 2.94m
  - `StopLossMultiplier` = 0.0025m
  - `TakeProfitMultiplier` = 0.022m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: ATR, SuperTrend
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
