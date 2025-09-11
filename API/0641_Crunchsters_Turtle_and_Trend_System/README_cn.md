# Crunchster's Turtle and Trend System 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合快慢 EMA 趋势过滤、Donchian 通道突破入场以及基于 ATR 的止损管理。另一个 Donchian 通道用于趋势反转时的退出。

## 细节

- **入场条件**：EMA 差值突破或 Donchian 通道突破。
- **方向**：多头和空头。
- **出场条件**：跟踪通道或 ATR 止损。
- **止损**：是，基于 ATR。
- **默认值**：
  - `CandleType` = 1 小时
  - `FastEmaPeriod` = 10
  - `BreakoutPeriod` = 20
  - `TrailPeriod` = 1000
  - `StopAtrMultiple` = 20
  - `OrderPercent` = 10
  - `TrendEnabled` = true
  - `BreakoutEnabled` = false
- **过滤器**：
  - 类别：趋势
  - 方向：多空皆可
  - 指标：EMA、Donchian、ATR
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
