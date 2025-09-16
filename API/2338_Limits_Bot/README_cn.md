# 限价机器人策略
[English](README.md) | [Русский](README_ru.md)

在每根K线开盘价附近对称挂入限价单，并使用止损、止盈和可选的跟踪止损保护持仓。

## 详情

- **入场**：
  - 若允许做多，在 `Open - StopOrderDistance * PriceStep` 处挂买入限价单。
  - 若允许做空，在 `Open + StopOrderDistance * PriceStep` 处挂卖出限价单。
- **出场**：止损、止盈或跟踪止损触发后以市价平仓。
- **多空方向**：双向。
- **止损**：固定止损并可启用跟踪。
- **默认值**：
  - `StopOrderDistance` = 5
  - `TakeProfit` = 35
  - `StopLoss` = 8
  - `TrailingStart` = 40
  - `TrailingDistance` = 30
  - `TrailingStep` = 1
  - `CandleType` = 1 分钟
- **交易时段**：仅在 `StartTime` 与 `EndTime` 之间交易。
- **筛选**：
  - 类别: 价格行为
  - 方向: 双向
  - 指标: 无
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
