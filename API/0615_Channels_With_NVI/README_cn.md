# Channels with NVI 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合布林带或凯尔特纳通道与负量指数（NVI）。当收盘价低于下轨且 NVI 高于其 EMA 时开多仓；当 NVI 低于其 EMA 时平仓。可选择设置止损和止盈百分比。

## 细节

- **入场条件**：
  - **多头**：收盘价 < 下轨且 NVI > NVI EMA。
- **多空方向**：仅多头。
- **离场条件**：
  - **多头**：NVI < NVI EMA。
- **止损**：可选，按入场价的百分比。
- **默认值**：
  - `ChannelType` = "BB"
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
  - `NviEmaLength` = 200
  - `EnableStopLoss` = false
  - `StopLossPercent` = 0
  - `EnableTakeProfit` = false
  - `TakeProfitPercent` = 0
- **过滤器**：
  - 分类：通道
  - 方向：多头
  - 指标：布林带或凯尔特纳通道、EMA、NVI
  - 止损：可选
  - 复杂度：低
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
