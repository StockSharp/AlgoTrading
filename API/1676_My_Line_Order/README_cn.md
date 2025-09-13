# My Line Order
[Русский](README_ru.md) | [English](README.md)

该策略在价格突破预设的水平线时发送市价单。用户分别指定做多和做空的触发价格以及以点为单位的风险参数。开仓后策略会跟踪止损、止盈以及可选的追踪止损。

策略适用于提前知道进场价格的情形。由于只依赖价格水平，可在任何品种和时间框架上使用。

## 详情

- **入场条件**：
  - **多头**：收盘价向上突破 `BuyPrice`。
  - **空头**：收盘价向下突破 `SellPrice`。
- **方向**：双向。
- **出场条件**：
  - `StopLossPips` 的止损。
  - `TakeProfitPips` 的止盈。
  - 当 `TrailingStopPips` > 0 时启用追踪止损。
- **止损**：是，以点计。
- **默认值**：
  - `BuyPrice` = 0（禁用）
  - `SellPrice` = 0（禁用）
  - `TakeProfitPips` = 30
  - `StopLossPips` = 20
  - `TrailingStopPips` = 0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：手动
  - 方向：双向
  - 指标：无
  - 止损：是
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
