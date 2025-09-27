# Flash Strategy Minervini Qualifier 策略
[English](README.md) | [Русский](README_ru.md)

结合EMA交叉、SuperTrend方向和动量RSI，并通过Minervini阶段分析进行过滤。

## 详情

- **入场条件**：EMA高于追踪线，SuperTrend趋势且动量RSI高于阈值，并满足Minervini阶段过滤
- **多头/空头**：均可
- **出场条件**：追踪线反向或SuperTrend翻转
- **止损**：无
- **默认值**：
  - `MomRsiLength` = 10
  - `MomRsiThreshold` = 60
  - `EmaLength` = 12
  - `EmaPercent` = 0.01
  - `SuperTrendPeriod` = 10
  - `SuperTrendMultiplier` = 3
- **过滤器**：
  - 分类：趋势
  - 方向：双向
  - 指标：EMA, SuperTrend, RSI
  - 止损：无
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
