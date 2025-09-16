# SmartAssTrade V2 Strategy
[English](README.md) | [Русский](README_ru.md)

SmartAssTrade V2策略在多重时间框架（1m、5m、15m、30m、60m）上使用MACD直方图和20周期移动平均线，并结合Williams %R和RSI过滤器捕捉趋势动量。可选的追踪止损用于保护利润。

## 细节

- **入场条件**：多数时间框架的MACD直方图和均线同时上升，并得到WPR/RSI确认
- **多空方向**：双向
- **出场条件**：价格触及止盈或止损；可选追踪止损
- **止损**：绝对止损与止盈，可选追踪
- **默认值**：
  - `Volume` = 1
  - `TakeProfit` = 35
  - `StopLoss` = 62
  - `UseTrailingStop` = false
  - `TrailingStop` = 30
  - `TrailingStopStep` = 1
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：MACD, SMA, Williams %R, RSI
  - 止损：可选
  - 复杂度：中等
  - 时间框架：多时间框架 (1m,5m,15m,30m,60m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
