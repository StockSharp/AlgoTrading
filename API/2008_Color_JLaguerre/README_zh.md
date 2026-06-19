# Color JLaguerre
[English](README.md) | [Русский](README_ru.md)

基于彩色 Laguerre 振荡器的策略。

该指标使用 Jurik 滤波器平滑价格，并根据相对水平着色。当颜色改变时，可能意味着趋势转向。

当振荡器向上穿越中线时策略做多，向下穿越时做空。达到极端水平或出现反向信号时平仓。

## 细节

- **入场条件**：Laguerre 振荡器在中线附近的颜色变化。
- **多头/空头**：双向。
- **出场条件**：反向信号或触及极端水平。
- **止损**：是。
- **默认值**：
  - `RsiLength` = 14
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**：
  - 类别：振荡器
  - 方向：双向
  - 指标：RSI
  - 止损：是
  - 复杂度：基础
  - 时间框架：1小时
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中
