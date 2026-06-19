# CP Strat ORB
[English](README.md) | [Русский](README_ru.md)

该策略在纽约开盘区间（9:30-9:45）突破并回测后入场。价格突破区间高点并回测后收于其上则做多；价格跌破区间低点并回测后收于其下则做空。止损和止盈为固定点数。

## 详情

- **入场条件**: 纽约开盘区间突破后回测并收盘突破。
- **多空方向**: 双向。
- **出场条件**: 固定止盈或止损。
- **止损**: 有。
- **默认值**:
  - `MinRangePoints` = 60m
  - `StopPoints` = 20m
  - `TakePoints` = 60m
  - `MaxTradesPerSession` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类别: Breakout
  - 方向: Both
  - 指标: None
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
