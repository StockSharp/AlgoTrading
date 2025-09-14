# 定时平仓策略

该策略在用户指定的本地时间自动关闭活动。
它能够根据多种筛选条件取消挂单并平掉持仓。

## 参数

- `CloseAll` – 启用后关闭所有订单和持仓。
- `CloseBySymbol` – 仅处理证券代码等于 `SymbolToClose` 的项目。
- `CloseByMagicNumber` – 关闭 `UserOrderId` 等于 `MagicNumber` 的订单。
- `CloseByTicket` – 关闭标识符为 `TicketNumber` 的特定订单。
- `ClosePendingOrders` – 取消未成交的限价/止损单。
- `CloseMarketOrders` – 通过市价单平掉持仓。
- `TimeToClose` – 开始执行平仓的本地时间。
- `SymbolToClose` – `CloseBySymbol` 的证券代码筛选。
- `MagicNumber` – 期望的 `Order.UserOrderId` 值。
- `TicketNumber` – 期望的订单标识符。

## 逻辑

启动后，策略会安排在 `TimeToClose` 执行一次性任务。
当时间到达时，按以下顺序运行：

1. 遍历所有活动订单并根据所选筛选条件检查。
2. 符合条件的挂单被取消。
3. 若启用 `CloseMarketOrders`，符合条件的持仓将通过市价单平掉。

该实现使用 StockSharp 高级 API，功能与原始 MQL 脚本在指定时间关闭订单的行为一致。

## 注意

- 若 `TimeToClose` 已过，平仓会立即开始。
- 筛选条件之间为逻辑或，`CloseAll` 会覆盖其它所有筛选。
- 由于平台差异，`MagicNumber` 与 `TicketNumber` 被视为字符串值。
