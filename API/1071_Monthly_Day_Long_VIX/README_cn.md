# Monthly Day Long VIX 策略
[English](README.md) | [Русский](README_ru.md)

本策略在每月指定日期且 VIX 指数低于阈值时开多仓。仓位在固定的柱数后或触发止损/止盈时平仓。

## 详情

- **入场条件**：
  - 到达 `EntryDay` 且 VIX < `VixThreshold`。
- **多空方向**：仅多头。
- **出场条件**：持有 `HoldDuration` 根K线后或由保护触发。
- **止损**：止损、止盈。
- **默认值**：
  - `EntryDay` = 27
  - `HoldDuration` = 4
  - `VixThreshold` = 20
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 5
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 分类：Seasonality
  - 方向：Long
  - 指标：VIX
  - 止损：止损、止盈
  - 复杂度：基础
  - 时间框架：日线
  - 季节性：是
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
