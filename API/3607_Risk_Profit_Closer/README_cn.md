# Risk Profit Closer 策略

## 概述
**Risk Profit Closer 策略** 持续监控当前交易品种，只要未实现盈亏达到账户权益的指定百分比，就立即平掉仓位。原始的 MetaTrader 脚本在每个 tick 上轮询，通过比较入场价与最新买卖价之间的差距来决定是否平仓。移植到 StockSharp 后，策略改为订阅 Level1 报价，但依旧执行相同的百分比阈值检查。

与常规的进场策略不同，本策略不会主动开仓。它的定位是与其他策略或手动交易配合使用，作为风险守护模块，在浮动盈亏触碰阈值时强制平仓。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `RiskPercentage` | 每个仓位可承受的最大亏损，占账户权益的百分比。 | `1` |
| `ProfitPercentage` | 达到该百分比收益时立即平仓。 | `2` |
| `TimerInterval` | 当市场无新报价时，重新执行检查的间隔。 | `00:00:01` |

## 交易逻辑
1. 启动时，策略会确认已经指定 `Security` 与 `Portfolio`，随后通过 `SubscribeLevel1()` 订阅买卖盘报价，并按照 `TimerInterval` 设置定时检查。
2. 每次收到 Level1 更新或定时器触发都会调用风险评估。评估逻辑读取当前账户权益（优先使用 `Portfolio.CurrentValue`，若无则退回 `Portfolio.BeginValue`），并把百分比参数转换成绝对金额。
3. 对于当前品种的仓位，策略使用最新 `BestBidPrice`/`BestAskPrice` 与入场价计算浮动盈亏。如果证券提供了 `PriceStep` 和 `StepPrice`，则会把价差换算成货币金额，否则按价差 × 数量估算。
4. 当浮动盈利大于等于 `ProfitPercentage × 权益`，或浮动亏损大于等于 `RiskPercentage × 权益` 时，通过 `ClosePosition` 立刻平仓。

## 移植说明
- MetaTrader 中的 `CheckTrades()` 遍历所有持仓并比较 `Symbol()`。移植版本通过 `Portfolio.Positions` 仅筛选与当前 `Security` 相同的仓位，行为一致。
- MetaTrader 在多头仓位使用 `symbolInfo.Ask()`、空头仓位使用 `symbolInfo.Bid()`。StockSharp 版本沿用这一差异化处理，优先采用最新的 `BestAskPrice`/`BestBidPrice`，若缺失则退回到最后成交价或最新价。
- 原脚本用价差乘以 `SymbolInfo.Point()` 来估算盈亏。StockSharp 中改用 `PriceStep` 和 `StepPrice` 来换算步长；若缺少这些数据，则退回到价差乘以数量的简单模型。
- 通过定时器模拟 MetaTrader 的 `OnTick` 循环，确保在行情停滞或报价稀疏时风险控制仍能触发。

## 使用建议
- 启动前务必设置 `Security` 与 `Portfolio`。若无法获取账户权益（`Portfolio.CurrentValue` 或 `Portfolio.BeginValue`），阈值就无法计算。
- 策略假设经纪商提供的是净持仓（单个品种只有一个多头或空头数量）。若存在套保式的多笔仓位，`ClosePosition` 会像原脚本一样平掉净头寸。
- 可以与其他交易策略同时运行；只有在达到盈亏阈值时才会发出平仓指令。
