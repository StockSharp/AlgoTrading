# 按时间开仓和平仓 v2 策略
[English](README.md) | [Русский](README_ru.md)

该策略基于时间，在指定的 `OpenTime` 开仓，并在 `CloseTime` 平仓。交易方向由快、慢指数移动平均线的比较决定。止损和止盈以跳数表示。

## 详情

- **入场条件**：在 `OpenTime`，若快 EMA 高于慢 EMA，则做多；若低于，则做空。具体方向由 `TradeMode` 控制。
- **多空方向**：可配置（做多、做空或双向）。
- **离场条件**：在 `CloseTime` 平仓或触发保护性止损/止盈。
- **止损止盈**：支持，均以跳数设置。
- **默认值**：
  - `OpenTime` = 05:00
  - `CloseTime` = 21:01
  - `SlowPeriod` = 200
  - `FastPeriod` = 50
  - `StopLossTicks` = 30
  - `TakeProfitTicks` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤条件**：
  - 分类：基于时间
  - 方向：可配置
  - 指标：EMA
  - 止损：固定
  - 复杂度：基础
  - 时间框架：日内（1 分钟）
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：低
