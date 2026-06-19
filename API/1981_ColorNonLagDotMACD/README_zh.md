# Color NonLag Dot MACD 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用 MACD 指标并提供多种信号模式，移植自 MQL 策略 "Exp_ColorNonLagDotMACD"。

## 详情

- **入场条件**：取决于选择的模式（零线突破、MACD 方向反转、信号线反转或 MACD 与信号线交叉）。
- **多空方向**：双向，可单独启用。
- **离场条件**：反向信号或设定的止损/止盈。
- **止损**：可选的百分比止损和止盈。
- **默认值**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Mode` = `MacdDisposition`
  - `TakeProfitPercent` = 4
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：MACD
  - 止损：是
  - 复杂度：中等
  - 时间框架：4H
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
