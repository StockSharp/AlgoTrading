# BreakThrough 策略
[English](README.md) | [Русский](README_ru.md)

BreakThrough 策略在价格突破用户设定的趋势线水平时开仓。
主要使用两个价格水平：
- **Buy Line** – 触发多头的价格。
- **Sell Line** – 触发空头的价格。

当价格从另一侧穿越这些水平时，策略按方向进场。
还可以设置附加的线，在价格触及时立即平仓。
保护性止损、止盈和追踪止损以入场价的点数表示。

## 详情

- **入场条件**：
  - **多头**：价格根据初始位置向上或向下突破 Buy Line。
  - **空头**：价格根据初始位置向上或向下突破 Sell Line。
- **多空方向**：双向。
- **出场条件**：
  - 价格触及附加的止盈或止损线。
  - 价格达到以点数表示的止盈或止损距离。
  - 触发追踪止损。
- **止损**：是，使用 `StopLossPips`、`TakeProfitPips` 和 `TrailingStopPips`。
- **默认值**：
  - `BuyLinePrice` = 0（关闭）
  - `SellLinePrice` = 0（关闭）
  - `TakeProfitPips` = 100
  - `StopLossPips` = 30
  - `TrailingStopPips` = 20
- **过滤器**：
  - 类型: 突破
  - 方向: 双向
  - 指标: 无
  - 止损: 是
  - 复杂度: 简单
  - 时间框架: 任意（默认 1 分钟）
  - 风险等级: 中等

