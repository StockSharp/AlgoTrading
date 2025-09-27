# Multi Regression 策略
[English](README.md) | [Русский](README_ru.md)

当价格穿越回归线时入场，并通过基于波动率的区间管理风险。可选的止损和止盈由所选的风险指标计算。

## 详情

- **入场条件**：价格与回归值交叉。
- **多空方向**：双向。
- **出场条件**：反向信号或价格到达区间。
- **止损**：可选，由 `UseStopLoss` 和 `UseTakeProfit` 控制。
- **默认值**：
  - `Length` = 90
  - `RiskMeasure` = Atr
  - `RiskMultiplier` = 1
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：Trend
  - 方向：双向
  - 指标：LinearRegression, ATR/StdDev/Bollinger/Keltner
  - 止损：可选
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
