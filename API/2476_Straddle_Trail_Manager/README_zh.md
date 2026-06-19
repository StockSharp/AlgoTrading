# Straddle Trail 策略

## 概述

**Straddle Trail** 策略复刻了 MetaTrader 5 "Straddle&Trail" 智能交易系统的核心逻辑。策略会在重大事件前或启动后立即在当前价格上下方挂出一组止损单（买入止损与卖出止损），在任一方向触发后，系统会负责移动止损到保本位、跟踪止损以及在需要时清理仓位或撤销挂单。

实现基于 StockSharp 的高级 API，全部功能通过 `Strategy` 基类提供的方法完成，无需手工处理底层消息。

## 交易流程

1. **布置双向止损单**
   * 当到达预设的事件窗口，或启用 `PlaceStraddleImmediately` 时，立即同时下达买入止损和卖出止损订单。
   * 每个订单的价格相对当前 Bid/Ask 偏移 `DistanceFromPrice` 个点（pip），偏移量通过合约的最小报价步长转换成价格。
   * 同一天内不会重复创建新的止损单，除非原有的订单被调整或取消。

2. **事件前的订单调整**
   * `AdjustPendingOrders` 为真时，系统会在每个新分钟撤销并重新下单，使止损单始终保持与市场价格对称。
   * 调整会在事件前 `StopAdjustMinutes` 分钟停止，以避免新闻发布前的剧烈波动。
   * `RemoveOppositeOrder` 启用后，一旦某一方向触发并建仓，另一侧未成交的止损单会自动撤销。

3. **仓位管理**
   * 初始止损/止盈距离由 `StopLossPips` 和 `TakeProfitPips` 决定并在策略内部跟踪。
   * 当浮动盈利达到 `BreakevenTriggerPips` 时，止损移动到开仓价并锁定 `BreakevenLockPips` 的利润（空头为对称处理）。
   * 若 `TrailPips` 大于 0，则会按照设定的距离跟踪止损；可选择在到达保本后才开始 (`TrailAfterBreakeven`)。
   * 止损和止盈通过市价单执行，以保证执行可靠性。

4. **手动停止**
   * 将 `ShutdownNow` 设为 `true` 会在下一根已完成的 K 线触发清理流程，具体动作为 `ShutdownMode` 指定的项目（平掉多/空仓位或撤销挂单）。

## 参数说明

| 参数 | 说明 |
|------|------|
| `ShutdownNow` | 在下一根 K 线执行清理流程，执行后自动复位为 `false`。 |
| `ShutdownMode` | 选择清理对象：`All`、`LongPositions`、`ShortPositions`、`PendingLong`、`PendingShort`。 |
| `DistanceFromPrice` | 买入/卖出止损距离当前价格的点数。 |
| `StopLossPips` | 初始止损距离，0 表示不设止损。 |
| `TakeProfitPips` | 初始止盈距离，0 表示不设止盈。 |
| `TrailPips` | 跟踪止损的距离，0 表示禁用。 |
| `TrailAfterBreakeven` | 为真时，仅在达到保本后才开始跟踪止损。 |
| `BreakevenLockPips` | 触发保本时锁定的利润点数。 |
| `BreakevenTriggerPips` | 激活保本逻辑所需的浮盈点数。 |
| `EventHour` / `EventMinute` | 计划事件的服务器时间，两个值均为 0 时禁用事件调度。 |
| `PreEventEntryMinutes` | 事件前多少分钟开始布置止损单，禁用事件或立即布置时忽略。 |
| `StopAdjustMinutes` | 事件前多少分钟停止自动调整挂单。 |
| `RemoveOppositeOrder` | 成交后撤销对侧止损单。 |
| `AdjustPendingOrders` | 启用自动对齐未成交的止损挂单。 |
| `PlaceStraddleImmediately` | 策略启动后立即布置双向止损。 |
| `CandleType` | 用于时间管理的 K 线数据类型（默认为 1 分钟）。 |

> **交易量**：策略的 `Volume` 属性决定下单数量，默认值为 1，可在启动前调整。

## 数据订阅

* 选择的 K 线序列（默认 1 分钟）用于调度事件、跟踪止损和执行停止指令。
* 订单簿数据用于获取最新的 Bid/Ask 价格，以精确定位止损单。

## 注意事项

* 为简化实现并保持与原版一致，止损和止盈通过市价单触发，而不是修改经纪商端的保护单。
* Pip 的换算使用 `PriceStep`，若交易品种的报价方式特殊，请相应调整参数。
* 停止指令在新 K 线到来时才会检查，如需更快响应可使用更短周期的 K 线。 
* 该策略仅管理自身下单产生的仓位，未处理外部/手工交易。
* 按要求未提供 Python 版本。

## 转换说明

* 保本和跟踪止损逻辑按照原始 MQL 代码逐步移植，但在 StockSharp 中采用内部变量并以市价单实现退出。
* 原 EA 使用魔术号区分仓位，这里依赖 StockSharp 的内部状态管理，不再生成独立的魔术号。

