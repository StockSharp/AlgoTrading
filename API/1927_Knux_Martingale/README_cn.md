# Knux Martingale Strategy 策略 (中文)
[English](README.md) | [Русский](README_ru.md)

该策略使用马丁格尔方法，在出现亏损后增加交易量。指标 Average Directional Index (ADX) 用于过滤信号，只在趋势市场中开仓。阳线触发做多，阴线触发做空。

## 细节

- **入场条件**：
  - ADX > 25
  - 多头：`Close > Open`
  - 空头：`Close < Open`
- **多/空**：双向
- **出场条件**：止损或止盈
- **止损**：有
- **默认参数**：
  - `AdxPeriod` = 14
  - `LotsMultiplier` = 1.5m
  - `StopLoss` = 150m
  - `TakeProfit` = 50m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 类别：趋势跟随，马丁格尔
  - 方向：双向
  - 指标：AverageDirectionalIndex
  - 止损：绝对
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：高
