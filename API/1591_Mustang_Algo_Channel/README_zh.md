# Mustang Algo Channel Strategy
[English](README.md) | [Русский](README_ru.md)

该策略使用基于 RSI 的全球情绪振荡器并通过 WMA 平滑，用于交易通道突破。

## 细节

- **入场条件**：RSI/WMA 振荡器与边界的交叉。
- **多空方向**：可配置。
- **出场条件**：反向信号或止损/止盈。
- **止损**：百分比，可选。
- **默认值**：
  - `RsiPeriod` = 14
  - `Smoothing` = 20
  - `MedianPeriod` = 25
  - `UpperBound` = 55
  - `LowerBound` = 48
  - `TradeMode` = Long & Short
  - `UseStopLoss` = true
  - `UseTakeProfit` = true
  - `StopLossPercent` = 4
  - `TakeProfitPercent` = 12
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 类别：趋势
  - 方向：可配置
  - 指标：RSI, WMA
  - 止损：百分比
  - 复杂度：中等
  - 时间框架：日线
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
