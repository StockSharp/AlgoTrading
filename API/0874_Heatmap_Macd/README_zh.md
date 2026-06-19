# MACD热图策略
[English](README.md) | [Русский](README_ru.md)

该系统使用来自五个时间框的MACD直方图热图。当所有直方图同时转为正或负时，按相应方向进场；若这种排列被打破或触发风险控制则离场。

## 详情

- **入场条件**: 所有MACD直方图在零线上方/下方。
- **多空方向**: 双向。
- **出场条件**: 直方图不再一致或触发止损。
- **止损**: 是。
- **默认值**:
  - `FastPeriod` = 20
  - `SlowPeriod` = 50
  - `SignalPeriod` = 50
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `CandleType1` = TimeSpan.FromMinutes(60)
  - `CandleType2` = TimeSpan.FromMinutes(120)
  - `CandleType3` = TimeSpan.FromMinutes(240)
  - `CandleType4` = TimeSpan.FromMinutes(240)
  - `CandleType5` = TimeSpan.FromMinutes(480)
- **过滤器**:
  - 类别: Momentum
  - 方向: Both
  - 指标: MACD
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Multi-timeframe
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
