# Time Trader 策略
[English](README.md) | [Русский](README_ru.md)

基于时间的策略，在指定的时刻根据设置选择做多或做空，并使用止盈止损保护头寸。

## 细节

- **入场条件**：在 `TradeHour:TradeMinute:TradeSecond` 时，如果 `AllowBuy` 为真则买入，如果 `AllowSell` 为真则卖出
- **多空方向**：根据设置可做多或做空
- **出场条件**：止盈或止损
- **止损**：是
- **默认值**：
  - `Volume` = 1
  - `TakeProfit` = 0.2
  - `StopLoss` = 0.2
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `TradeSecond` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `CandleType` = TimeSpan.FromSeconds(1).TimeFrame()
- **过滤器**：
  - 分类: Time
  - 方向: 双向
  - 指标: 无
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

