# 按账户货币的盈利或亏损平仓

该策略移植自 MetaTrader 专家顾问 *Close_on_PROFIT_or_LOSS_inAccont_Currency*。它持续监控所连接投资组合的当前权益值，一旦达到设定的盈利目标或跌破亏损底线，就会取消所有挂单并平掉全部持仓。实现基于 StockSharp 的高级 API：蜡烛订阅提供“心跳”，`CancelActiveOrders()` 撤销挂单，而 `ClosePosition()` 通过市价单完成平仓。

## 工作流程

1. 每当心跳蜡烛收盘时，读取 `Portfolio.CurrentValue`。
2. 若权益值大于或等于 **Positive Closure**，触发完整的退出流程。
3. 若权益值小于或等于 **Negative Closure**，执行同样的流程以限制亏损。
4. 在退出过程中，策略会撤销挂单、提交反向市价单并停止自身运行（对应原版中的 `ExpertRemove()`）。

> **注意：** 阈值以账户货币表示。为了避免启动后立即触发，请将 **Positive Closure** 设置在当前权益之上，并将 **Negative Closure** 设置在其下方。

## 参数

| 名称 | 说明 | 默认值 |
|------|------|--------|
| `PositiveClosureInAccountCurrency` | 当权益达到或超过该值时立即平仓。 | `0` |
| `NegativeClosureInAccountCurrency` | 当权益跌至该值或更低时立即平仓。 | `0` |
| `CandleType` | 提供心跳的蜡烛周期。若需更快响应，可选择更短的时间框。 | `1 分钟` |

## 说明

- 启动时会调用 `StartProtection()`，以保持原策略的防护逻辑。
- 策略仅处理自身管理的仓位和订单，请将其附加到需要保护的投资组合。
- 未单独提供点差或滑点参数，因为 StockSharp 的市价单会根据连接器自动处理这些因素。
