# Double MA Breakout Strategy

## 概述
**Double MA Breakout Strategy** 是 MetaTrader 智能交易系统 `DoubleMA_Breakout` 的 StockSharp 版本。策略在每根已完成的 K 线上计算一条快线和一条慢线移动平均。当快线突破慢线时，会在最近收盘价上方按指定的突破距离挂出 Buy Stop；当快线跌破慢线时，则在下方挂出 Sell Stop。一旦信号反转或者交易窗口结束，所有挂单都会被撤销，已有仓位将被平掉。

移植过程保留了原始 EA 的核心逻辑，引入了 StockSharp 的高阶下单与风控能力，并通过 `StrategyParam<T>` 提供了细致的参数控制。代码中的注释全部改写为英文。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `FastMaPeriod` | 2 | 快速均线周期。 |
| `SlowMaPeriod` | 5 | 慢速均线周期。 |
| `FastMaMode` | `Simple` | 快速均线的计算方式（SMA、EMA、SMMA、LWMA、LSMA）。 |
| `SlowMaMode` | `Simple` | 慢速均线的计算方式。 |
| `FastAppliedPrice` | `Close` | 快速均线使用的价格类型（收盘、开盘、最高、最低、中值、典型价、加权价）。 |
| `SlowAppliedPrice` | `Close` | 慢速均线使用的价格类型。 |
| `SignalShift` | 1 | 评估信号时向前查看的已完成 K 线数量，`0` 表示当前 K 线。 |
| `BreakoutDistancePoints` | 45 | 以价格步长为单位的突破距离，用于放置挂单。 |
| `UseTimeWindow` | `true` | 是否启用交易时段过滤。 |
| `StartHour` | 11 | 允许开仓的起始小时（含）。 |
| `StopHour` | 16 | 停止交易的截止小时（含）。 |
| `UseFridayCloseAll` | `true` | 周五到达指定时间后平掉所有仓位并撤销挂单。 |
| `FridayCloseTime` | 21:30 | 周五强制平仓的时间。 |
| `UseFridayStopTrading` | `false` | 周五到达指定时间后禁止新的开仓，但保留现有仓位。 |
| `FridayStopTradingTime` | 19:00 | 周五停止开仓的时间（在启用时生效）。 |
| `CandleType` | 1 小时 | 用于计算信号的 K 线类型。 |

## 交易流程
1. 订阅 `CandleType` 对应的完成 K 线，按照所选模式与价格源计算两条移动平均。
2. 维护短期历史队列，用来访问 `SignalShift` 指定的那根 K 线，避免使用被禁止的 `GetValue` 调用。
3. **看多条件：** 快速均线高于慢速均线时，撤销 Sell Stop，平掉空头仓位，并在收盘价上方 `BreakoutDistancePoints × PriceStep` 的位置挂出 Buy Stop（前提是没有其他挂单或持仓）。
4. **看空条件：** 快速均线低于慢速均线时，撤销 Buy Stop，平掉多头仓位，并在对称位置下方挂出 Sell Stop。
5. **时间控制：** 超出交易时段时，所有挂单会被撤销。周五可选地在“停止开仓时间”后禁止新单，并在“收盘时间”强制清仓，避免隔夜风险。
6. 任一挂单成交后，另一侧的挂单会自动撤销，防止出现双向持仓。

## 与原始 EA 的差异
- 原策略中的资金管理选项和多级追踪止损未移植。下单手数由 StockSharp 的 `Volume` 属性决定，额外的风险控制可通过平台保护模块实现。
- MetaTrader 中的低级接口、错误重试逻辑被高层 API (`BuyStop`、`SellStop`、`ClosePosition`、`CancelOrder`) 取代，代码更加简洁稳健。
- 诸如保证金阈值、滑点修正等经纪商特定功能未包含在内，如有需要可自行扩展。
- LSMA 模式使用 StockSharp 的 `LinearRegression` 指标来模拟 MetaTrader 的最小二乘均线。

## 使用建议
- 启动前请设置合适的 `Volume`；若保持默认值，则只交易一个合约/手。 |
- 代码中已调用 `StartProtection()`，可以在平台层面附加止损或止盈模块。 |
- 通过构造函数中的 `.SetCanOptimize` 设置，可将目标参数加入优化流程。 |
- 请确认交易标的具有正确的 `PriceStep`；若返回零，则会退化为默认步长 `1`，以避免挂单距离为零。 |
