# Locker 对冲网格策略

该策略复刻 MetaTrader 4 专家顾问 **Locker.mq4** 的行为。每个交易循环都会先发送一笔市价买单，然后同时管理买单与卖单组成的对冲网格。当所有持仓的浮动利润累计到账户权益的一定比例时，策略会立即平掉全部头寸并进入下一个循环。如果浮亏达到同样的比例，策略会按照固定的点数间隔逐步加仓买入或卖出，对价格波动进行“锁仓”处理。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `NeedProfitRatio` | 达到或亏损到该比例的权益时触发平仓/加仓逻辑。`0.001` 代表账户权益的 0.1%。 | `0.001` |
| `InitialVolume` | 每个循环开始时首笔市价买单的手数。 | `0.5` |
| `StepVolume` | 进入救援阶段后，每次加仓使用的手数。 | `0.2` |
| `StepPoints` | 相邻救援单之间的 MetaTrader 点数距离。策略会利用 `Security.PriceStep` 将其转换成价格。 | `50` |
| `EnableRescue` | 当浮亏超过阈值时是否启动加仓网格。关闭后策略只保留初始仓位等待盈利。 | `true` |

## 交易流程

1. **循环起点**
   - 收到第一笔成交行情后立刻以 `InitialVolume` 手买入。
   - 建仓价会记录为检查点，同时重置“最高买价”和“最低卖价”追踪器。

2. **利润锁定**
   - 每次行情更新都会计算所有多头与空头的浮动盈亏。多头贡献 `(price - averageBuyPrice) * longVolume`，空头贡献 `(averageSellPrice - price) * shortVolume`。
   - 当浮盈达到 `NeedProfitRatio * equity` 时，通过相反方向的市价单平掉全部仓位，待成交确认后开启新一轮循环。

3. **救援网格**
   - 当浮亏跌破 `-NeedProfitRatio * equity` 且 `EnableRescue` 为真时，策略会等待价格从最近检查点移动 `StepPoints` 点（已换算为价格）。突破新高触发一笔救援买单，跌破新低触发救援卖单，手数均为 `StepVolume`。
   - 每次加仓后都会刷新检查点与方向性极值，确保下一次加仓必须再走完一个完整的点差距离。

4. **循环复位**
   - 通过 `OnOwnTradeReceived` 监控到多头和空头仓位均归零后，等待状态解除，检查点与极值重置为最新成交价，策略即可重新发送首笔买单。

## 实现细节

- 采用 `SubscribeTrades().Bind(ProcessTrade)` 订阅逐笔成交，贴近原 EA 直接使用买卖价的特点。
- 利用 `Security.PriceStep` 推导出 MT4 的“点 (pip)”大小，若合约报价保留 3 位或 5 位小数，则自动乘以 10 进行调整。
- 在 `OnOwnTradeReceived` 中分别维护多头与空头的库存和均价，从而支持像 MT4 一样的对冲持仓（多空可以同时存在）。
- 权益阈值优先使用 `Portfolio.CurrentValue`，若不可用则回退到 `CurrentBalance` 或 `BeginValue`，并缓存第一份正值，保证计算稳定。
- 所有市价订单都会通过 `AlignVolume` 辅助方法，符合 `VolumeStep`、`VolumeMin` 与 `VolumeMax` 的限制。

## 使用建议

- 请确认合约元数据提供了正确的 `PriceStep`，否则点值换算会失真，网格距离无法与 MetaTrader 保持一致。
- 救援逻辑类似马丁格尔，请谨慎设置 `StepVolume`，同时关注风险敞口。加大 `StepPoints` 与 `StepVolume` 会减少订单次数但放大仓位。
- 将 `EnableRescue` 设为 `false` 可以得到保守版本：只开第一笔仓位并等待盈利，不再做任何摊平操作。
- 建议在外汇等品种上使用逐笔行情回测，以重现 EA 的原始粒度。

## 与原版 EA 的差异

- 原脚本在单数超过 8 笔时尝试成对平仓，但由于票据筛选的缺陷从未触发，因此在移植时被省略。
- 初始化阶段根据既有订单重新计算 `StepLot` 的逻辑未保留；手数完全由 StockSharp 参数控制。
- EA 中的下单备注、弹窗提示以及手动停止标志均未移植，StockSharp 版本专注于自动交易流程。
