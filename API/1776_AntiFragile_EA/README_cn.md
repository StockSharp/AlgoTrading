# AntiFragile EA 策略
[English](README.md) | [Русский](README_ru.md)

一个网格策略，在当前价格上下按固定间隔挂单并逐级增加手数。
仓位先设置固定止损，并在价格向有利方向运行时通过追踪止损保护利润。

## 细节

- **入场**：
  - 多头：在每个 `SpaceBetweenTrades` 间隔下方挂 buy limit。
  - 空头：在每个 `SpaceBetweenTrades` 间隔上方挂 sell limit。
- **多空方向**：通过 `TradeLong` 和 `TradeShort` 控制。
- **离场**：追踪止损或反向网格触发。
- **止损**：固定 `StopLossPips` 与追踪 `TrailingStopPips`。
- **默认参数**：
  - `StartingVolume` = 0.1m
  - `IncreasePercentage` = 1m
  - `SpaceBetweenTrades` = 700m
  - `NumberOfTrades` = 50
  - `StopLossPips` = 300m
  - `TrailingStopPips` = 100m
  - `TradeLong` = true
  - `TradeShort` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **过滤器**：
  - 类别：网格
  - 方向：双向
  - 指标：无
  - 止损：追踪
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：高
