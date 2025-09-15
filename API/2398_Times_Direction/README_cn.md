# Times Direction 策略
[English](README.md) | [Русский](README_ru.md)

该时间驱动策略在预定时间窗口内开仓一次多头或空头，并在另一个时间窗口内平仓。入场方向可配置，同时监控可选的止损和止盈。策略仅使用已完成的K线，不依赖指标。

## 细节

- **入场条件**：
  - 当当前K线时间位于 `[OpenTime, OpenTime + TradeInterval)` 且没有持仓时，按设定方向开仓。
- **离场条件**：
  - 当时间位于 `[CloseTime, CloseTime + TradeInterval)` 时平仓。
  - 如果达到止损或止盈水平也会平仓。
- **多空方向**：可设置。
- **止损止盈**：以价格单位设置，相对于入场价。
- **默认值**：
  - `Trade` = Sell。
  - `OpenTime` = 1970-01-01 00:00。
  - `CloseTime` = 3000-01-01 00:00。
  - `TradeInterval` = 1 分钟。
  - `StopLoss` = 1000。
  - `TakeProfit` = 2000。
  - `Volume` = 0.1。
- **过滤器**：
  - 分类：基于时间
  - 方向：单向
  - 指标：无
  - 止损：是
  - 复杂度：简单
  - 时间框架：短期
