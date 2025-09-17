# MultiHedg 1（定时多货币对冲）

## 概述
- 将 MetaTrader 4 专家顾问 **MultiHedg_1** 移植到 StockSharp 的高级策略框架。
- 通过统一的时间计划同时处理最多十个外汇品种的入场信号。
- 交易者可手动选择整组仓位的方向（买入或卖出），用于构建对冲篮子。

## 交易逻辑
1. **定时入场窗口**：当当前蜡烛的时间落入区间 `[TradeHour:TradeMinute, TradeHour:TradeMinute + DurationSeconds]` 时，对每个启用的品种发送市价单。如果账户中存在反向净头寸，会增加下单量，以保证最终净头寸等于参数中设定的手数。
2. **可选的定时平仓**：若启用 `UseCloseTime`，同样的持续时间会应用到退出窗口（`CloseHour:CloseMinute`）。在该时间段内，策略会对所有受管品种调用 `ClosePosition` 进行平仓。
3. **权益保护**：`CloseByPercent` 使用组合当前权益 (`Portfolio.CurrentValue`) 与最近一次空仓时记录的余额进行比较。当权益上升超过 `PercentProfit`% 或下跌低于 `PercentLoss`% 时，立即清空篮子中的全部仓位。
4. **余额跟踪**：每次强制平仓之后（或所有品种回到空仓时）都会刷新参考余额，从而与原 EA 基于 `AccountBalance()` 的行为保持一致。

## 参数
| 组别 | 参数 | 说明 |
|------|------|------|
| Orders | `Sell` | `true` 时全部开空，否则全部开多。 |
| Schedule | `TradeHour`, `TradeMinute` | 入场窗口的起始时间（终端时间）。 |
| Schedule | `DurationSeconds` | 入场与出场窗口共用的持续秒数。 |
| Schedule | `UseCloseTime` | 是否启用定时平仓。 |
| Schedule | `CloseHour`, `CloseMinute` | 平仓窗口的起始时间。 |
| Risk | `CloseByPercent` | 启用权益百分比保护。 |
| Risk | `PercentProfit`, `PercentLoss` | 相对于参考余额的盈利/亏损阈值。 |
| Data | `CandleType` | 用于驱动时间检查的蜡烛周期（建议 1 分钟以模拟 MT4 的逐笔循环）。 |
| Symbols | `UseSymbolN` | 是否启用第 N 个品种（默认前六个开启）。 |
| Symbols | `SymbolN` | 指定给该槽位的 `Security`，需在启动前配置。 |
| Symbols | `SymbolNVolume` | 该槽位的下单数量（默认 0.1 … 1.0 手）。 |

## 使用提示
- 对所有 `UseSymbolN = true` 的槽位提前指定有效的 `Security` 对象，未启用的槽位会被忽略。
- 策略未移植 MT4 的 “magic number” 机制，只会对列表中的品种发起平仓。
- 请确保所有品种的 `CandleType` 订阅相同的时间框架，以获得与原始 EA 一致的调度时间。
- 权益保护依赖于连接器提供的 `Portfolio.CurrentValue`/`BeginValue` 数据，需保证适配器能返回这些字段。
- 根据需求，本策略暂不提供 Python 版本。
