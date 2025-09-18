# XP Trade Manager Grid（StockSharp 版本）

## 策略概述
原版 **XP Trade Manager Grid** 专家顾问在价格向不利方向移动固定点数时加仓，形成一条
至多 15 单的网格。每个仓位都带有 `order1`、`order2` … `order15` 的注释，并拥有独立的
止盈规则：前 3 个级别使用固定点数，4–15 级则在加权持仓成本之上叠加预设的总利润。
当组合利润达到目标时，所有仓位一次性平仓。EA 还会累计 `order1` 获得的收益，如果最近
一次止盈后累计收益仍低于设定值，就会自动重新开出 `order1`。

本移植版本完全依赖仓库推荐的高阶 API——通过 `SubscribeCandles().Bind(...)` 订阅蜡烛数
据，只把完成的蜡烛当成调度事件。每根蜡烛收盘后，策略读取最新的买价/卖价，并根据距
离和收益条件做出决策。

## 执行流程
1. **订阅数据**：根据参数 `CandleType` 订阅蜡烛。策略不需要任何指标。
2. **前 3 级止盈**：若 `order1`、`order2` 或 `order3` 达到各自的止盈距离
   （`TakeProfit1Partitive`、`TakeProfit2`、`TakeProfit3`），即刻提交反向市价单（带
   `orderN_tp` 注释）仅平掉对应级别。
3. **扩展网格**：当最新仓位亏损达到 `AddNewTradeAfter` 点，并且尚未达到 `MaxOrders`
   上限时，自动发送下一层 `order{N+1}`。内部集合会追踪正在注册的级别，避免重复报单。
4. **组合止盈**：当持仓数量达到 4 单及以上时，计算当前方向的加权平均成本，并按照
   `TakeProfitXTotal` 指定的总利润（除以持仓数量）得到目标价。一旦市场价穿越该水平，
   使用 `breakeven_exit` 注释的市价单整体平仓。
5. **风险监控**：每根蜡烛重新估算浮动盈亏。如果亏损超过账户价值的 `RiskPercent`%，
   立即用 `risk_exit` 注释平掉全部仓位。
6. **首单续开**：`order1` 被止盈或强制平仓后，策略会记录方向、止盈价以及获得的点数和
   货币收益。如果累计收益仍低于 `TakeProfit1Total`，且市场价格距离最近的止盈价至少有
   `TakeProfit1Offset` 个点，将在同方向重新开出新的 `order1`。该逻辑可通过
   `AutoRenewFirstOrder` 参数关闭。

## 参数对应
| MetaTrader 输入            | StockSharp 参数             | 说明 |
|---------------------------|-----------------------------|------|
| `AddNewTradeAfter`        | `AddNewTradeAfter`          | 新增网格订单所需的最小逆向点数。 |
| `TakeProfit1Partitive`    | `TakeProfit1Partitive`      | `order1` 的止盈距离。 |
| `TakeProfit2`             | `TakeProfit2`               | `order2` 的止盈距离。 |
| `TakeProfit3`             | `TakeProfit3`               | `order3` 的止盈距离。 |
| `TakeProfit4Total` … `15` | `TakeProfitXTotal`          | 当持仓数量为 N 时需要累计的总利润点数。 |
| `TakeProfit1Total`        | `TakeProfit1Total`          | `order1` 止盈累计目标，用于控制自动续开。 |
| `TakeProfit1Offset`       | `TakeProfit1Offset`         | 最近止盈价与市场价的最小距离，满足后才允许续开 `order1`。 |
| `MaxOrders`               | `MaxOrders`                 | 每个方向最多允许的网格数量。 |
| `Risk`                    | `RiskPercent`               | 允许的最大浮亏，占投资组合价值的百分比。 |
| `Lots`                    | `OrderVolume`               | 每次加仓的下单量。 |
| —                         | `CandleType`                | 触发管理循环的蜡烛类型。 |
| —                         | `AutoRenewFirstOrder`       | 是否启用首单自动续开。 |

所有点数会通过 `Security.PriceStep` 转换为价格距离，`Security.StepPrice` 则用于将点数换算成货
币盈亏；若行情未提供这些属性，代码会使用保守的默认值，确保逻辑依旧有效。

## 实现细节
- 始终使用 `BuyMarket`、`SellMarket` 等高阶下单方法，订单注释沿用 MQL 版本，方便在成交
  列表中对照。
- 通过集合 `_pendingBuyStages` / `_pendingSellStages` 管控正在注册的级别，防止在蜡烛间隔内
  重复触发同一层加仓。
- 浮动盈亏根据价格步长手动计算，即便经纪商不提供实时的未实现盈亏，也能及时触发风控。
- 首单续开的逻辑与 EA 一致：只有在累计收益未达 `TakeProfit1Total` 且价格离开最近止盈价
  足够远时才会重启网格首单。

## 与原版 EA 的差异
- MetaTrader 图表上的文本（累计点数、货币收益）未移植，但所有数据在策略内部均有记录，
  可以通过日志或自定义界面输出。
- 采用蜡烛收盘事件代替逐笔 Tick。如果使用 1 分钟蜡烛，行为与原版非常接近，同时符合仓
  库对于高阶 API 的要求。
- 止盈通过市价单完成，而不是修改仓位 TP/SL。这使策略与不同的经纪商适配更好，也符合
  StockSharp 的设计理念。

## 使用建议
1. 绑定组合和标的，配置好网格间距、止盈阶梯和风险参数。
2. 维持 `AutoRenewFirstOrder = true` 可以让策略在首单止盈后自动重启网格；如需人工控制，
   可以关闭该选项并手动下发首单。
3. 启动策略。所有加仓、止盈和风险退出都会自动完成，只需根据需要监控整体仓位即可。

## 风险提示
网格策略可能在短时间内放大敞口。上线实盘前务必确认 `MaxOrders`、`OrderVolume` 和
`RiskPercent` 等参数的安全性，并建议通过回测验证不同波动场景下的表现。
