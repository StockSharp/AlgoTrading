# Zone Recovery Button 策略

**Zone Recovery Button Strategy** 源自 MetaTrader 专家顾问 "ZONE RECOVERY BUTTON VER1"（`MQL/25347`）。
原版 EA 通过图表上的 BUY/SELL 按钮手动启动网格。移植到 StockSharp 后，按钮被参数化替代，但保留了原有的
恢复对冲逻辑、货币/百分比止盈、利润回撤追踪以及基于权益的保护。

当设置了起始方向后，策略会立即开立第一笔市价单。当价格穿越预设的区间宽度时，系统会以更大的手数
加仓反向单。以下情况会关闭整个篮子：达到点数止盈、实现指定的货币或百分比收益、追踪止盈出现超额
回撤、或浮动亏损突破权益保护阈值。

## 交易规则

1. **起始方向** —— 模拟按下 BUY/SELL 按钮。收到行情并允许交易后立即建立首单，平仓后可按需自动重新启动。
2. **区间恢复** —— 每次加仓都交替方向。多头循环中，价格跌破 `基准价 - 区间宽度` 时卖出对冲，回到基准价上方后再买入；空头循环逻辑相反。
3. **手数扩张** —— 新的对冲单可以按倍数放大前一单的手数，也可以在禁用倍数时按固定增量叠加，对应 EA 的
`Lots` 与 `Multiply` 设置。
4. **出场条件** —— 篮子在以下情形关闭：
   - 价格触及点数止盈；
   - 浮动利润超过 `MoneyTakeProfit`；
   - 浮动利润超过账户价值的 `PercentTakeProfit`%；
   - 启动追踪后利润回撤超过允许范围；
   - 浮亏超过 `TotalEquityRiskPercent`%（相对于本轮最高权益）。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `CandleType` | `TimeSpan.FromMinutes(5)` | 用于监控价格的蜡烛类型。 |
| `StartDirection` | `Buy` | 初始方向（BUY/SELL/NONE）。 |
| `AutoRestart` | `true` | 篮子平仓后是否自动按照同一方向重新启动。 |
| `TakeProfitPips` | `200` | 基准价到点数止盈之间的距离。 |
| `ZoneRecoveryPips` | `10` | 触发下一笔对冲单的区间宽度。 |
| `InitialVolume` | `0.01` | 第一笔订单的手数。 |
| `UseVolumeMultiplier` | `true` | 是否按照倍数扩张手数。 |
| `VolumeMultiplier` | `2` | 倍数扩张时的乘数。 |
| `VolumeIncrement` | `0.01` | 禁用倍数时每次增加的手数。 |
| `MaxTrades` | `100` | 每个篮子允许的最大订单数量。 |
| `UseMoneyTakeProfit` | `false` | 是否启用货币止盈。 |
| `MoneyTakeProfit` | `40` | 货币止盈阈值。 |
| `UsePercentTakeProfit` | `false` | 是否启用百分比止盈。 |
| `PercentTakeProfit` | `10` | 百分比止盈阈值。 |
| `EnableTrailing` | `true` | 是否开启利润追踪。 |
| `TrailingProfitThreshold` | `40` | 启动追踪所需的最低利润。 |
| `TrailingDrawdown` | `10` | 追踪启动后允许的利润回撤。 |
| `UseEquityStop` | `true` | 是否启用权益保护。 |
| `TotalEquityRiskPercent` | `1` | 权益回撤阈值（占本轮最高权益的百分比）。 |

## 说明

- 需要交易品种提供 `PriceStep` 与 `StepPrice`，策略才能把点数转换成价格与货币金额。
- StockSharp 采用净额持仓模型，因此策略内部维护一份虚拟的网格队列来复现 MetaTrader 的收益计算方式。
- 追踪功能仅监控篮子的浮动盈亏，不会自动挂出附加的止损单。
