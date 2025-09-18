# Pedro Mod 策略

## 概述

本策略为 **Pedroxxmod** MetaTrader 4 智能交易系统的 StockSharp 版本。原始 EA 会在价格相对参考价偏离若干点
后建立逆势仓位，并在价格回撤既定距离时继续在同一方向加仓。移植后的实现保留了这一核心思想，同时
通过高层 `Strategy` API 暴露出强类型参数，便于回测与优化。

## 交易逻辑

1. 订阅 Level1 买一/卖一报价，并缓存最新的买卖价。
2. 当没有持仓时，把当前卖价作为参考价。只有当服务器时间位于 `StartHour` 与 `EndHour` 之间，并且年份
   不早于 `StartYear` 时才允许交易。
3. 如果卖价高于参考价 `Gap` 个 MetaTrader 点，则提交市价卖单；如果卖价低于参考价 `Gap` 点，则提交市
   价买单。下单后立即通过 `SetStopLoss`/`SetTakeProfit` 设置与 EA 相同的止损和止盈距离。
4. 一旦确定交易方向，策略会使用先进先出队列跟踪“虚拟仓位”，从而模拟 MetaTrader 的对冲账户。当篮子
   中的交易数量小于 `MaxTrades` 时，只要卖价重新回到最近一次入场价 `ReEntryGap` 点以内，就会继续在相同
   方向加仓。
5. 仓位管理既可以使用固定的 `Lots`，也可以启用 money management：按照 `floor(Equity / 20000)` 计算手数，并
   受 `MaxLots` 限制。最终下单量会根据交易品种的最小变动/最小手数进行规范化。
6. 在交易时段之外到达的报价会重置参考价，防止下一交易时段误触发订单。

## 参数

| 名称 | 说明 |
|------|------|
| `Lots` | 禁用 money management 时使用的固定下单量。 |
| `StopLoss` | MetaTrader 点为单位的止损距离，设为 `0` 表示不使用止损。 |
| `TakeProfit` | MetaTrader 点为单位的止盈距离，设为 `0` 表示不使用止盈。 |
| `Gap` | 卖价相对参考价需要偏离的点数，满足后才会开出第一笔仓位。 |
| `MaxTrades` | 同方向同时存在的最大订单数量（篮子大小）。 |
| `ReEntryGap` | 触发继续加仓的回撤距离（MetaTrader 点）。 |
| `MoneyManagement` | 置为 `true` 时启用 `floor(Equity / 20000)` 的动态手数算法。 |
| `MaxLots` | 动态手数的上限。 |
| `StartHour` / `EndHour` | 服务器时间下的交易时段（包含边界）。 |
| `StartYear` | 允许交易的最早年份，更早的数据会被忽略。 |

## 备注

- 策略只订阅 Level1 行情，不请求任何 K 线，因此能够像 MT4 的 `start()` Tick 处理函数那样实时响应。
- 止损与止盈通过 `Strategy` 的辅助函数来换算实际价格，确保接入方正确提供 `PriceStep`、`StepPrice` 和
  `VolumeStep` 等交易参数。
- 通过 FIFO 队列维护的“虚拟仓位”可以模拟对冲账户，即便 StockSharp 采用净头寸模型也不会丢失 EA 的行为。
  局部成交或止损都会在 `OnPositionChanged` 回调中更新该队列。
- 根据仓库要求，暂不提供 Python 版本。
