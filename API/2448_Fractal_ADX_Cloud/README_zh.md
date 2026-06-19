# Fractal ADX Cloud
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 中使用平均趋向指数（ADX）指标，复现原始 MQL `Fractal_ADX_Cloud` 专家的思想。策略基于四小时K线，分析 +DI 与 -DI 分量的交叉。当 +DI 上穿 -DI 时，策略平掉空头并可开多头；当 -DI 上穿 +DI 时则反向操作开空。

止损和止盈以绝对价格单位设置，可分别启用或禁用多空方向的开仓和平仓。

## 细节

- **入场条件**：ADX 的 +DI/-DI 交叉。
- **多空方向**：双向。
- **离场条件**：反向信号或止损/止盈。
- **止损**：是，绝对价格。
- **默认值**：
  - `AdxPeriod` = 30
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 分类：趋势
  - 方向：双向
  - 指标：ADX
  - 止损：有
  - 复杂度：基础
  - 周期：4小时
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
