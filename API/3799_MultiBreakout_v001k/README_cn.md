# MultiBreakout V001k 策略

## 概述
MultiBreakout V001k 策略完整复刻 MT4 智能交易系统 “Multibreakout_v001k”。它在参考交易小时结束后，依据刚刚收盘的小时 K 线高低点，成批挂出买入止损和卖出止损订单，并保留原版的分批止盈与保本移动逻辑（含可选的移动保本功能）。

## 交易规则
1. **参考交易时段** – 最多可定义四个交易时段。每个启用的时段在对应小时结束后，策略都会读取该小时的完成 K 线数据，并为下一小时准备突破订单。
2. **挂单逻辑** –
   - 买入止损放在上一小时最高价加上当前点差与额外进场缓冲 (`PipsForEntry`)。
   - 卖出止损放在上一小时最低价减去进场缓冲。
   - 每个方向会同时挂出 `NumberOfOrdersPerSide` 个等量订单。
3. **分批止盈** – 每个进场订单都会按照 `TakeProfitIncrement` 点的固定间隔设置独立的止盈目标。当价格触及目标时，策略会市价平仓一份，效果与原 MT4 版本的止盈队列一致。
4. **止损管理** – 初始止损距离进场价 `StopLoss` 点。当浮动盈利达到 `BreakEven` 点时，止损移动到保本位。如果启用 `MovingBreakEven` 且延迟 (`MovingBreakEvenHoursToStart`) 已到，策略会根据最近两个已完成小时 K 线的低点/高点（多/空）进一步收紧止损。
5. **时段离场** – 在参考时段内达到 `ExitMinute` 时，策略会立即平掉所有头寸并撤销所有挂单。

## 参数说明
| 参数 | 说明 |
|------|------|
| `TradeVolume` | 每个突破挂单的下单量。 |
| `NumberOfOrdersPerSide` | 每个方向同时挂出的订单数量。 |
| `TakeProfitIncrement` | 连续止盈目标之间的点差。 |
| `PipsForEntry` | 在突破触发价上/下额外添加的进场缓冲。 |
| `StopLoss` | 初始止损距离。 |
| `BreakEven` | 触发保本移动所需的盈利点数。 |
| `MovingBreakEven` | 是否启用移动保本逻辑。 |
| `MovingBreakEvenHoursToStart` | 在参考时段结束后等待多少小时才允许移动保本。 |
| `BrokerOffsetToGmt` | 经纪商服务器相对于 GMT 的小时偏移，用于对齐移动保本时间表。 |
| `TradeSession1..4` | 四个独立交易时段的启用开关。 |
| `SessionHour1..4` | 每个参考时段的小时（0-23）。 |
| `ExitMinute` | 参考时段内触发全平的分钟数。 |
| `CandleType` | 用于测量参考小时的 K 线类型（默认 1 小时）。 |

## 使用提示
- 请确保标的设置了正确的 `PriceStep`，以便点值计算与 MT4 行为保持一致。
- 策略假定 K 线时间与经纪商服务器时间一致，如需匹配历史 MT4 服务器，请调整 `BrokerOffsetToGmt`。
- 移动保本会参考最近两个完成的小时 K 线，只有当最新低点/高点持续收窄时才会跟随调整止损。
