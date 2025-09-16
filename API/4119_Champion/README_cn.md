# 4119 Champion 策略

## 概览
本策略是位于 `MQL/919/champion.mq5` 的 MetaTrader 专家顾问的 C# 高级 API 改写版。原始 EA 在 RSI 指标出现信号后同步挂出三个止损单，并为每个挂单预先设置止损/止盈；价格朝有利方向移动时再跟踪止损。本移植版本完全依赖 StockSharp 的高级接口（`SubscribeCandles`、`Bind`、`BuyStop`、`SellStop` 等）实现相同的流程。

默认参数适用于点值与 `PriceStep` 相同的外汇品种（例如欧元/美元，点值 0.0001）。蜡烛类型可配置，只要账户能提供最优买卖价以及（可选的）止损距离信息，就可以在任意周期使用。

## 策略流程
1. **信号生成**
   - 在已完成的蜡烛上计算指定长度的 RSI。
   - 取上一根闭合蜡烛的 RSI 数值，与对称阈值 `RsiLevel` 比较。
   - `RSI < RsiLevel` 触发做多准备；`RSI > 100 - RsiLevel` 触发做空准备。
2. **挂出止损单**
   - 当没有持仓且策略没有在管的挂单时，同时挂出三个方向一致的止损单。
   - 买入止损单挂在最佳卖价之上，卖出止损单挂在最佳买价之下；距离优先遵守券商返回的 stop level，如无则采用 `MinOrderDistancePoints` 作为后备。
   - 下单手数按“账户可用权益 ÷ `BalancePerLot`”计算，结果限制在 0.1 至 15 手之间并保留两位小数，每个挂单分得总手数的三分之一。
3. **初始保护单**
   - 首笔成交后，立即按照持仓方向注册聚合的止损/止盈订单：止损放在 `成交价 ± StopLossPoints`，止盈放在 `成交价 ± TakeProfitPoints`（MetaTrader 点值乘以 `PriceStep`）。
   - `TakeProfitPoints = 0` 时不下达止盈单。
4. **跟踪止损**
   - 持仓期间，每次收到 level-1 行情都会尝试上调止损价。
   - 多头新的止损价为 `max(建仓价 + spread, Bid - StopLoss)`；空头为 `min(建仓价 - spread, Ask + StopLoss)`。
   - 只有当价格至少超过“券商止损距离 + 当前点差”时才会移动止损，以保持与原 EA 一致的安全限制。
5. **挂单维护**
   - 当买入止损价格距离当前卖价超过 `RepriceDistancePoints` 时，会撤单并在更接近的位置重新挂出；卖出止损对买价执行同样逻辑。
   - 重新挂单的距离取 `RepriceDistancePoints` 与有效 stop level 的较大值，确保不会违反交易所限制。
6. **离场**
   - 仓位通过保护单（止损或止盈）或人工处理平仓；仓位归零后立即撤销残余保护单并等待新的 RSI 信号。

## 参数
| 参数 | 说明 |
|------|------|
| `TakeProfitPoints` | 以 MetaTrader 点值表示的止盈距离，0 表示禁用止盈。 |
| `StopLossPoints` | 以点值表示的止损距离，同时用于计算跟踪止损。 |
| `RsiPeriod` | RSI 的计算周期数。 |
| `RsiLevel` | 对称阈值，低于该值准备做多，高于 `100 - RsiLevel` 准备做空。 |
| `BalancePerLot` | 计算动态手数时认为一手对应的账户金额。 |
| `MinOrderDistancePoints` | 当券商未返回 stop level 时使用的最小挂单距离（点）。 |
| `RepriceDistancePoints` | 触发重新挂单的距离阈值（点）。 |
| `CandleType` | 用于计算 RSI 的蜡烛类型。 |

## 使用说明
- 策略需要蜡烛数据和 level-1 行情（最优买卖价）。没有 level-1 行情时不会执行挂单维护和跟踪止损。
- 如果券商通过 level-1 数据提供 stop level 或 stop distance，会自动采用该距离；否则需自行设置 `MinOrderDistancePoints`。
- 当无法获取投资组合信息或计算出的手数≤0 时，会退回到 `Strategy.Volume` 指定的固定手数。
- 每次都会同时挂出三张方向一致的止损单。如需部分挂单，可手动删除多余的订单，策略仍会管理剩余订单。

## 风险控制
- 止损/止盈使用真实的委托，效果与原 MetaTrader EA 完全一致；仓位关闭后会立即撤销对应保护单。
- 跟踪止损只向盈利方向移动，不会放宽止损；必须满足“价格超出建仓价至少 `(StopLevel + spread)`”这一条件后才会调整。
- 挂单重置逻辑可避免行情跳空后遗留过远的挂单，降低迟滞成交的风险。
