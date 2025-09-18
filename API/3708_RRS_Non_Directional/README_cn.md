# RRS Non-Directional 策略

## 概述
本策略将 MetaTrader 4 的 “RRS Non-Directional” 智能交易系统迁移到 StockSharp 框架。原始版本根据所选模式同时建立买入和卖出网格，并通过虚拟的止损、止盈与跟踪止损来管理仓位。StockSharp 版本保留了全部可配置模式、资金风险控制以及虚拟保护机制，同时适配 StockSharp 的净额结算组合结构。因此，在所谓的“对冲”模式下，仓位会在多空之间交替，而不是像 MT4 那样同时持有两个方向。

## 交易逻辑
- 订阅 Level-1 行情以获得最新买价和卖价，并在每次准备入场前对比 `MaxSpreadPoints` 限制。
- 入场逻辑由 `TradingMode` 控制：
  - `HedgeStyle` 与 `AutoSwap` 通过在多空之间轮换来模拟双向网格（StockSharp 无法同时保留独立的买单与卖单）。
  - `BuySellRandom` 每次机会都会随机选择方向。
  - `BuySell` 始终开出与最近一次平仓相反的方向。
  - `BuyOrder` 与 `SellOrder` 将交易限制在单一方向。
- `AllowNewTrades` 对应 MT4 的 `New_Trade` 变量，可随时阻止新的市场单。
- 所有订单都使用 `TradeVolume` 指定的基础数量，并在订单中写入 `TradeComment` 以便外部平台识别。

## 风险控制与离场
- 止损与止盈距离以 MetaTrader 点数表示，并通过 `PriceStep` 转换成实际价格差，因而无需针对不同品种手动调整。
- `StopMode`、`TakeMode` 与 `TrailingMode` 在关闭、虚拟和“经典”之间切换。在 StockSharp 实现中，只要不是关闭，保护逻辑都会以虚拟方式执行：一旦触发条件，就通过市价单平仓，从而保证跨平台表现一致。
- 当价格向盈利方向运行超过 `TrailingStartPoints` 时，策略会启动跟踪止损，并保持距离为 `TrailingGapPoints`。
- 每次 Level-1 更新都会重新计算未实现盈亏。如果亏损超过 `RiskMode` 与 `MoneyInRisk` 定义的阈值，仓位会立刻被强制平掉。

## 参数说明
| 参数 | 说明 |
|------|------|
| `TradingMode` | 入口模式，源自原始 EA。由于净额结算，所谓的对冲模式会在多空之间交替。 |
| `AllowNewTrades` | 是否允许开立新的市场单。 |
| `TradeVolume` | 每次下单的基础数量。 |
| `StopMode` | 止损管理方式（`Disabled`、`Virtual`、`Classic`）。 |
| `StopLossPoints` | 以 MetaTrader 点数表示的止损距离。 |
| `TakeMode` | 止盈管理方式（`Disabled`、`Virtual`、`Classic`）。 |
| `TakeProfitPoints` | 以 MetaTrader 点数表示的止盈距离。 |
| `TrailingMode` | 跟踪止损管理方式（`Disabled`、`Virtual`、`Classic`）。 |
| `TrailingStartPoints` | 激活跟踪止损所需的盈利点数。 |
| `TrailingGapPoints` | 跟踪止损启动后保持的点数间隔。 |
| `RiskMode` | 解释 `MoneyInRisk` 时所采用的模式：余额百分比或绝对金额。 |
| `MoneyInRisk` | 风险阈值，未实现盈亏低于该值时立即清仓。 |
| `MaxSpreadPoints` | 允许开仓时的最大点差。 |
| `SlippagePoints` | 为保持与原始输入一致而保留的滑点信息参数。 |
| `TradeComment` | 写入到订单的备注。 |

## 注意事项
- MT4 中的 AutoSwap 依赖于经纪商提供的隔夜利息数据。大部分 StockSharp 连接器不会在 Level-1 中给出这些值，因此该模式会自动降级为 `HedgeStyle` 并记录提示。
- “经典”止损、止盈与跟踪止损在此版本中同样通过虚拟方式完成。如果需要真正的挂单保护，应在更底层自行实现。
- 由于 StockSharp 对每个工具只维护一个净仓位，策略在对冲模式下无法同时持有双向仓，实际表现为多空交替，这一点需要在回测与实盘中特别注意。
