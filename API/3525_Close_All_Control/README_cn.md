# CloseAllControlStrategy

## 概述

**CloseAllControlStrategy** 是从 `MQL/38245/CloseAll.mq4` 移植到 StockSharp 的 MetaTrader "CloseAll" 批量管理工具。策略在启动后立刻执行一次性操作，根据配置关闭符合条件的持仓并/或撤销挂单，适用于需要快速清仓或删除特定订单的多品种组合管理场景。

该策略不会订阅行情，也不会等待新的 Tick。它只读取当前投资组合的状态，完成所选动作，然后立即停止自身。因此可以在 Designer 或 AlgoTrading 中作为“紧急关闭”按钮或收盘助手。

## 参数

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `OrderComment` | `string` | `"Bonnitta EA"` | 需要在订单或持仓评论中出现的子串（忽略大小写）。为空时不启用评论过滤。 |
| `Mode` | `CloseAllMode` | `CloseAll` | 选择要执行的批量操作场景，具体行为见下表。 |
| `CurrencyId` | `string` | 空 | 仅在按品种处理的模式中使用。为空时回退到策略绑定的 `Security`。 |
| `MagicOrTicket` | `long` | `1` | 与订单编号或策略标识比较的数字。只有正数才会触发相关过滤逻辑。 |

### 模式矩阵

| 模式 | 持仓处理 | 挂单处理 | 额外过滤条件 |
|------|-----------|-----------|----------------|
| `CloseAll` | 关闭所有符合条件的多头和空头持仓。 | — | 仅评论过滤。 |
| `CloseBuy` | 仅关闭多头持仓。 | — | 仅评论过滤。 |
| `CloseSell` | 仅关闭空头持仓。 | — | 仅评论过滤。 |
| `CloseCurrency` | 关闭 `CurrencyId`（或当前策略品种）对应的持仓。 | — | 评论 + 品种过滤。 |
| `CloseMagic` | 关闭标识与 `MagicOrTicket` 匹配的持仓。 | — | 评论 + 魔术号。 |
| `CloseTicket` | 关闭与 `MagicOrTicket` 相同编号的单一持仓。 | — | 评论 + 单号。 |
| `ClosePendingByMagic` | — | 撤销魔术号匹配的挂单。 | 评论 + 魔术号。 |
| `ClosePendingByMagicCurrency` | — | 撤销同时满足魔术号与品种条件的挂单。 | 评论 + 魔术号 + 品种。 |
| `CloseAllAndPendingByMagic` | 关闭魔术号匹配的持仓。 | 撤销对应挂单。 | 评论 + 魔术号。 |
| `ClosePending` | — | 撤销所有符合评论过滤的挂单。 | 仅评论过滤。 |
| `CloseAllAndPending` | 关闭全部持仓。 | 撤销全部挂单。 | 仅评论过滤。 |

### 标识匹配规则

* `MagicOrTicket` 会依次与 `Order.TransactionId`、`Order.Id`、`Order.UserOrderId` 以及（若可转换为数字）`StrategyId` 进行比较。
* 优先进行文化无关的数字比较；如果无法解析为数字，则把 `MagicOrTicket` 的十进制字符串与目标字段做大小写不敏感比较。
* 小于或等于 0 的值将禁用所有魔术号/单号过滤。

### 评论过滤

* 在比较之前，过滤字符串和目标评论都会去除首尾空格。
* 比较采用大小写不敏感的子串匹配，与原始 MQL `StringFind` 行为一致。
* 持仓评论通过反射读取；若无法获取评论，则该持仓不会通过过滤，与 MetaTrader 中评论不匹配时的表现一致。

## 执行流程

1. 确认已绑定投资组合，并调用一次 `StartProtection()`。
2. 根据所选模式筛选需要处理的持仓和挂单。
3. 发送相应的市价平仓或撤单指令。
4. 完成后立即停止策略。

策略不会重复创建保护性订单，也不会订阅任何行情数据；所有操作都通过 `BuyMarket`、`SellMarket` 与 `CancelOrder` 等高级 API 完成。

## 使用建议

* 启动前先设置好投资组合和（如需）策略绑定的交易工具。
* 调整 `OrderComment` 以匹配希望处理的 EA 注释或人工备注。
* 如果只想处理某个具体合约，可在 `CurrencyId` 中填入对应的安全标识，例如 `EURUSD@FORTS`。
* `MagicOrTicket` 应设置为实际使用的标识；若不需要按编号过滤，可设置为 `0`。
* 策略执行完毕会自动停止，可放入 Designer 流程中按需触发，避免重复平仓。

## 与原始 MQL 脚本的差异

* 图形按钮（“Close Orders”、“Exit”）被替换为启动后即时执行的逻辑。
* 挂单类型通过 `OrderTypes` 判断，覆盖 StockSharp 支持的所有非市价类型。
* 通过反射读取持仓与订单的评论及策略标识，以提升与不同券商适配器的兼容性。
* 日志输出依赖基类 `Strategy` 的标准机制（未使用 `Print`），如有需要可自行添加日志。
