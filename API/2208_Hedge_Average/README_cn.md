# Hedge Average 策略

该策略源自 MetaTrader 的 "Hedge Average" 专家。它比较两个周期内开盘价与收盘价的简单移动平均线。

## 交易逻辑

- 计算 `Period1` 和 `Period2` 的开盘价与收盘价 SMA。
- 当长周期开盘均线高于其收盘均线且短周期开盘均线低于其收盘均线时，开多单。
- 当长周期开盘均线低于其收盘均线且短周期开盘均线高于其收盘均线时，开空单。
- 只有在 `StartHour` 与 `EndHour` 之间才允许交易。
- 可选的止损和止盈以绝对价格单位设置，启用时 trailing stop 会随着价格移动。

## 参数

- `Period1` – 快速均线周期。
- `Period2` – 慢速均线周期。
- `StartHour` – 开始交易的小时。
- `EndHour` – 结束交易的小时。
- `CandleType` – 使用的K线周期。
- `TakeProfit` – 止盈距离（价格单位）。
- `StopLoss` – 止损距离（价格单位）。
- `UseTrailing` – 是否启用基于止损距离的追踪止损。

## 说明

该策略采用单一持仓，不包含原版 MQL 中基于金额的获利目标。
