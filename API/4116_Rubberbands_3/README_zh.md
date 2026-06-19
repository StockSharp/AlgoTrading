# Rubberbands 3 策略

本策略是 MetaTrader 4 专家顾问 **RUBBERBANDS_3** 的 StockSharp 版本。算法跟踪价格的最高值与最低值，当价格按设定的点数向外扩张时逐步加仓，并在出现指定幅度的回撤时一次性平掉整组仓位。如果触发了反向信号，策略会在平仓后尝试在相反方向重新建立仓位，同时监控本交易日的累计盈亏。

> **提示：** StockSharp 使用净持仓模式。原始的 MT4 脚本可以同时持有多笔多头和空头，而此移植版本会在开仓反向之前先把当前方向的仓位全部平掉，从而保持整体行为一致。

## 交易逻辑

1. 启动时记录当前收盘价作为初始的最高价和最低价，或使用手工指定的 `InitialMax`、`InitialMin`。
2. 当价格较最高价上涨 `PipStep` 个点时，以 `OrderVolume` 的数量买入，并把最高价更新为最新值。
3. 当价格较最低价下跌 `PipStep` 个点时，以 `OrderVolume` 的数量卖出，并把最低价更新为最新值。
4. 如果行情出现 `BackStep` 个点的回撤，则立即平掉当前方向的全部仓位，并准备在另一方向重新开始建仓，前提是原有仓位已经全部退出。
5. 同时跟踪累计盈亏：当已实现利润加未实现利润达到 `SessionTakeProfit × OrderVolume` 时，关闭整场交易；如果在反向过程中浮动亏损超过 `SessionStopLoss × OrderVolume`，同样触发强制平仓。
6. `QuiesceNow` 让策略在空仓时保持静默，`StopNow` 会暂停所有逻辑，`CloseNow` 请求立即平仓。

策略基于 `CandleType` 指定的收盘完成的 K 线进行决策，默认使用 1 分钟周期，与原始 EA 每分钟检查一次的行为一致。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `OrderVolume` | 每次市场订单的基础手数。 | `0.02` |
| `MaxOrders` | 单一方向允许同时持有的最多仓位数。 | `10` |
| `PipStep` | 触发加仓的点数距离。 | `100` |
| `BackStep` | 触发整组平仓和准备反向的回撤距离。 | `20` |
| `QuiesceNow` | 为空仓且为 `true` 时禁止重新开仓。 | `false` |
| `DoNow` | 启动后立即打开第一笔多单。 | `false` |
| `StopNow` | 暂停所有决策逻辑，保留已有仓位。 | `false` |
| `CloseNow` | 请求尽快平掉所有仓位。 | `false` |
| `UseSessionTakeProfit` | 启用累计盈利目标。 | `true` |
| `SessionTakeProfit` | 以账户货币表示的单手盈利目标。 | `2000` |
| `UseSessionStopLoss` | 启用累计亏损限制。 | `true` |
| `SessionStopLoss` | 在反向时允许的最大亏损（按单手计算）。 | `4000` |
| `UseInitialValues` | 重新启动时是否使用手工输入的极值。 | `false` |
| `InitialMax` | 当 `UseInitialValues` 为 `true` 时使用的最高价。 | `0` |
| `InitialMin` | 当 `UseInitialValues` 为 `true` 时使用的最低价。 | `0` |
| `CandleType` | 用于计算的 K 线类型，默认 1 分钟。 | `TimeFrame(1m)` |

## 会话管理

- **利润累计：** 每次整组平仓后，把实现盈亏加入 `_realizedProfit`，未实现盈亏按剩余仓位的加权平均价即时计算。
- **会话止盈：** 达到 `SessionTakeProfit` 后，策略会关闭所有仓位并重置最高价、最低价。
- **会话止损：** 反向过程中若浮亏超过 `SessionStopLoss`，立刻平仓并重新开始下一轮。

## 使用建议

- 点值换算依赖 `Security.PriceStep`，如果该字段为空会退回使用 `0.0001`。
- 由于净持仓机制，策略会先平仓再在反向方向开仓，因此报表可能与原版的对冲模式存在差异。
- `DoNow` 仅影响第一笔交易，后续加仓完全遵循价格突破逻辑。
- 若需要在平仓后暂时停用策略，可将 `QuiesceNow` 设置为 `true`。

