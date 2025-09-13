# AI Grid Strategy
[English](README.md) | [Русский](README_ru.md)

AI Grid Strategy 在当前价格上下布置多层买卖订单。策略同时支持突破（停损）和逆势（限价）方式。每当订单成交后，会自动挂出固定距离的止盈单。

## 细节

- **入场条件**：价格到达某个网格层。
- **多空方向**：通过 `AllowLong` 和 `AllowShort` 控制。
- **出场条件**：距离 `TakeProfit` 的止盈。
- **止损**：无止损。
- **默认值**：
  - `GridSize` = 50m
  - `GridSteps` = 10
  - `TakeProfit` = 50m
  - `AllowLong` = true
  - `AllowShort` = true
  - `UseBreakout` = true
  - `UseCounter` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 分类：网格
  - 方向：双向
  - 指标：无
  - 止损：仅止盈
  - 复杂度：中等
  - 时间框架：日内 (1m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
