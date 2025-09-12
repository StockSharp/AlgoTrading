# TradingViewTo Strategy Template With Dynamic Alerts
[English](README.md) | [Русский](README_ru.md)

该模板策略基于RSI水平开仓，并通过百分比止损和止盈管理交易。

## 细节
- **入场条件**：
  - **多头**：RSI > `UpperLevel`
  - **空头**：RSI < `LowerLevel`
- **多空方向**：双向
- **出场条件**：
  - 止损或止盈
- **止损**：百分比止损和止盈
- **默认参数**：
  - `RsiLength` = 14
  - `UpperLevel` = 60
  - `LowerLevel` = 40
  - `StopLossPct` = 2m
  - `TakeProfitPct` = 4m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：动量
  - 方向：双向
  - 指标：RSI
  - 止损：是
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
