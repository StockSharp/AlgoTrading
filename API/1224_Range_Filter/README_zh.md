# 范围过滤器
[English](README.md) | [Русский](README_ru.md)

范围过滤器策略采用平滑的范围计算，并使用固定的止损和止盈。

通过计算平滑范围，在价格周围形成动态带。当价格突破这些带的上方或下方时入场。风险管理使用固定点数的止损和止盈。

## 详情

- **入场条件**: 价格突破范围过滤器带。
- **多空方向**: 双向。
- **退出条件**: 止损或止盈。
- **止损**: 有。
- **默认值**:
  - `SamplingPeriod` = 100
  - `RangeMultiplier` = 3
  - `RiskPoints` = 50
  - `RewardPoints` = 100
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: Range filter
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
