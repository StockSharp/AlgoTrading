# 多品种套保调度策略

## 概述
**Multi Hedging Scheduler Strategy** 是将 MetaTrader 5 指标 `MultiHedg_1.mq5` 迁移到 StockSharp 的版本。策略面向支持对冲的账户，可同时管理最多十个交易品种。在可配置的交易时间窗口内按统一方向开仓，并可根据时间或权益百分比阈值一次性平掉所有仓位。

策略不依赖技术指标，而是订阅（默认）一分钟蜡烛，仅把蜡烛的收盘时间当作调度触发器。每根完成的蜡烛会触发：
- 是否需要在交易窗口内开仓；
- 是否达到权益止盈/止损目标；
- 是否进入强制平仓时间窗口。

## 运行逻辑
1. **品种选择**：通过 `UseSymbolX` 参数启用最多十个交易品种。对每个启用的符号，策略通过 `SecurityProvider` 查找标的并订阅所选 `CandleType` 的蜡烛数据。
2. **交易窗口**：当蜡烛时间进入 [`TradeStartTime`, `TradeStartTime + TradeDuration`) 区间时，若当前方向的仓位尚未建立，策略按 `TradeDirection` 方向发送市价单。若账户中存在反向仓位，将追加足够的数量以翻转方向。
3. **权益保护**：启用 `CloseByEquityPercent` 后，策略会将实时权益与启动时的基准余额比较。当收益超过 `PercentProfit` 或回撤超过 `PercentLoss` 时，所有受管仓位都会被平掉。
4. **时间平仓**：启用 `UseTimeClose` 后，当时间进入 [`CloseTime`, `CloseTime + TradeDuration`) 区间时，策略会强制平掉全部仓位。
5. **日志记录**：所有关键事件（开仓、权益保护触发、时间平仓）都会通过 `LogInfo` 记录，便于回溯。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `TradeDirection` | 全部订单的方向（`Buy` 或 `Sell`）。 | Buy |
| `TradeStartTime` | 交易窗口开始时间。 | 19:51 |
| `TradeDuration` | 交易窗口与平仓窗口的持续时间。 | 00:05:00 |
| `UseTimeClose` | 是否启用时间平仓窗口。 | true |
| `CloseTime` | 平仓窗口开始时间。 | 20:50 |
| `CloseByEquityPercent` | 是否按权益百分比执行全局平仓。 | true |
| `PercentProfit` | 达到该收益百分比时平掉全部仓位。 | 1.0 |
| `PercentLoss` | 达到该回撤百分比时平掉全部仓位。 | 55.0 |
| `CandleType` | 作为调度驱动的蜡烛类型。 | 1 分钟 |
| `UseSymbol0..9` | 是否启用对应品种。 | 0–5 启用，6–9 关闭 |
| `Symbol0..9` | 每个槽位的标的代码（`SecurityProvider.LookupById` 使用的 ID）。 | 见下表 |
| `Volume0..9` | 每个槽位的下单数量。 | 0.1–1.0 |

**默认品种配置**

| 槽位 | 启用 | 品种 | 数量 |
|------|------|------|------|
| 0 | ✔ | EURUSD | 0.1 |
| 1 | ✔ | GBPUSD | 0.2 |
| 2 | ✔ | GBPJPY | 0.3 |
| 3 | ✔ | EURCAD | 0.4 |
| 4 | ✔ | USDCHF | 0.5 |
| 5 | ✔ | USDJPY | 0.6 |
| 6 | ✖ | USDCHF | 0.7 |
| 7 | ✖ | GBPUSD | 0.8 |
| 8 | ✖ | EURUSD | 0.9 |
| 9 | ✖ | USDJPY | 1.0 |

## 使用建议
- 如果需要复制 MT5 的对冲行为，请确认账户允许多方向同时持仓。在净额账户中，策略会自动补量以翻转到目标方向。
- `SymbolX` 参数必须与数据源中的标的 ID 完全一致（例如 `EURUSD@FXCM`）。
- 蜡烛数据仅作为时钟驱动，如需不同的更新频率可修改 `CandleType`。
- 每次启动策略都会重新记录起始余额，后续的权益阈值比较基于该数值。
- 策略不包含逐单止盈止损，全部退出逻辑由时间窗口和权益百分比共同控制。

## 转换说明
- 原 MT5 版本在 `OnTick` 中运行；StockSharp 版本改为基于完成的蜡烛触发，从而使用更高层级的事件绑定 API。
- 不再需要 `magic number` 过滤；`CloseAllManagedPositions` 只会遍历在参数中启用的品种。
- 原策略的声音提示和图表注释未迁移，取而代之的是详细的 `LogInfo` 日志。
