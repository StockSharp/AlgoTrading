# DLM v1.4 网格策略

## 概述
本策略是对 Alejandro Galindo 开发的 MetaTrader 4 专家顾问 “DLM v1.4” 的 StockSharp 版本。原始机器人利用 Fisher 变换信号过滤与马丁格尔加仓体系，当价格朝不利方向移动时逐步构建网格头寸。移植版本保留了原策略的资金管理思想，并将执行与保护逻辑改写为使用 StockSharp 的高级 API（K 线订阅、指标绑定以及市价/限价下单辅助方法）。

## 交易逻辑
- 读取设定周期的已完成 K 线，计算两个指标：Fisher 变换以及 Fisher 数值的 SMA 平滑。
- 根据两条曲线的相对位置判断篮子方向。当 Fisher 向上穿越平滑线时准备做多，向下穿越时准备做空。`ReverseSignals` 参数可反转这一判断。
- 当方向信号出现且自动交易开启（`ManualTrading = false`）时，立即通过市价单开出第一笔仓位。
- 在篮子持仓期间，只要价格相对于最近一次成交出现 `GridDistancePips` 的不利波动，就继续加仓。依据 `UseLimitOrders` 开关，可以选择在下一根 K 线收盘时使用市价单加仓，或提前挂出与上次成交价相距一个网格步长的限价单。
- 新增仓位的手数遵循原始马丁格尔模型：当 `MaxTrades > 12` 时，每次乘以 1.5，否则翻倍。基础手数可以固定在 `LotSize`，也可以在启用 `UseMoneyManagement` 后根据账户权益动态计算。
- 每次成交都会重新计算整个篮子的止损与止盈，使所有仓位共享同一组保护价位。若开启移动止损，当价格顺势移动超过 `GridDistancePips + TrailingStopPips` 时，止损会按照 `TrailingStopPips` 的距离跟随。

## 账户保护
- **盈利保护**（`SecureProfitProtection`）：当开仓数达到 `OrdersToProtect` 时，计算篮子的未实现盈亏（账户货币），若达到 `SecureProfit` 阈值则立即平掉所有仓位。
- **权益保护**（`EquityProtection` + `EquityProtectionPercent`）：持续监控投资组合当前权益，一旦低于策略启动时权益的指定百分比即平仓。
- **货币回撤保护**（`AccountMoneyProtection` + `AccountMoneyProtectionValue`）：当与初始权益相比的货币回撤超过设定数值时停止交易。
- **存续期保护**（`OrdersLifeSeconds`）：限制最近一笔成交的最长持有时间，超过限制后平掉全部仓位并终止本轮马丁格尔。
- **周五过滤器**（`TradeOnFriday`）：关闭后禁止在周五开启新的篮子。

所有保护性退出均使用市价单，确保能够成交；触发保护或重置网格时会自动撤销所有挂着的限价单。

## 参数
| 参数 | 说明 |
|------|------|
| `TakeProfitPips` | 应用于整个篮子的统一止盈距离（点数）。 |
| `StopLossPips` | 每笔新仓位的初始止损距离（点数）。 |
| `TrailingStopPips` | 触发后使用的移动止损距离。 |
| `MaxTrades` | 篮子允许的最大加仓次数。 |
| `GridDistancePips` | 决定下一笔加仓所需的最小不利波动（点数）。 |
| `LotSize` | 在未启用资金管理时使用的基础手数。 |
| `UseMoneyManagement` | 启用后根据原始风控公式动态计算手数。 |
| `RiskPercent` | 动态基础手数所使用的风险百分比。 |
| `AccountType` | 动态手数的账户类型缩放（0 标准、1 迷你、2 微型）。 |
| `SecureProfitProtection` | 是否启用浮动盈利保护。 |
| `SecureProfit` | 触发盈利保护所需的未实现盈亏（货币单位）。 |
| `OrdersToProtect` | 启动盈利保护前所需的最少开仓数。 |
| `EquityProtection` | 是否启用权益百分比保护。 |
| `EquityProtectionPercent` | 相对于启动权益的保护百分比。 |
| `AccountMoneyProtection` | 是否启用货币回撤保护。 |
| `AccountMoneyProtectionValue` | 允许的最大货币回撤。 |
| `TradeOnFriday` | 是否允许在周五开启新篮子。 |
| `OrdersLifeSeconds` | 最近一次成交允许的最长持仓时间（秒）。 |
| `ReverseSignals` | 反转 Fisher 信号方向。 |
| `UseLimitOrders` | 选择加仓时使用市价单还是限价单。 |
| `ManualTrading` | 设为 true 时关闭自动开仓。 |
| `CandleType` | 用于指标计算的时间框架。 |
| `FisherLength` | Fisher 变换的窗口长度。 |
| `SignalSmoothing` | 对 Fisher 数值进行平滑的 SMA 周期。 |
| `DefaultPipValue` | 将未实现盈亏换算为货币时使用的备用点值。 |

## 说明
- 按照仓库要求，源代码中的注释全部使用英文。
- 策略完全依赖 StockSharp 的高级接口（`SubscribeCandles`、`Bind`、`BuyLimit`、`SellLimit` 等），不直接操作指标缓存。
- 资金管理仍沿用原始风控公式，同时通过 `Security.ShrinkVolume` 与 `Security.ShrinkPrice` 调整手数与价格，以符合标的合约规格。
- 由于 StockSharp 与 MetaTrader 的执行细节不同（例如平仓统一使用市价单），移植版本在保持原始行为的同时做了必要的适配。
