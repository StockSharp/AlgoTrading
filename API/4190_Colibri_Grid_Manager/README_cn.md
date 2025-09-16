# Colibri 网格管理策略

## 概述
Colibri 网格管理策略将 MetaTrader 4 智能交易系统 `Colibri.mq4`（源目录 `MQL/9713`）迁移到 StockSharp。策略面向半自动网格交易：根据需要批量挂出分层委托、按照风险预算调整手数、自动附加保护性止损/止盈，并在达到日度亏损阈值后停止下单。

## 交易逻辑
1. 启动时订阅所选 K 线和订单簿，用于获取基准价，并清空上一轮残留的订单，同时记录当日的盈亏基准。
2. 当 `EnableGrid` 为真且没有仓位或活跃网格订单时，会针对允许的方向（`AllowBuy`、`AllowSell`）构建新的网格。网格既可以围绕手动指定的中心价分布，也可以以独立的买/卖锚点为起点。
3. `OrderType` 控制每个层级采用限价、止损还是市价方式建仓。`LevelSpacingPoints` 以“点”为单位设置层与层之间的距离，程序会结合合约最小变动价位转换成绝对价格差。
4. 手数可固定（`FixedOrderVolume`），也可以通过 `RiskPercent` 按风险比例动态计算。风险模式会取当前账户权益乘以风险比例，再除以本方向层数以及止损带来的货币风险，从而得到每笔委托的手数。
5. 入场单成交后会自动挂出成对的保护性订单：止损价优先采用 `StopLossPrice`，若未填写则根据 `StopDistancePoints` 计算；止盈价取 `TakeProfitDistancePoints`，若为零则默认使用一个网格步长。待挂单可在 `ExpirationHours` 小时后失效。
6. 策略持续跟踪已实现 + 浮动盈亏。一旦当日亏损超过 `DailyLossLimitPercent` 设定的权益比例，立即撤销所有委托、平掉仓位，并暂停开新网格，直到下一交易日开始。
7. 提供 `CloseAllPositions`、`CloseLongPositions`、`CloseShortPositions`、`CancelOrders` 等手动开关，方便在界面上一键清仓或清单。

## 参数说明
- **EnableGrid**：总开关，控制是否自动维护网格。
- **OrderType**：网格采用的委托类型（限价、止损或市价）。
- **AllowBuy / AllowSell**：指定允许参与网格的方向。
- **UseCenterLine / CenterPrice**：启用后按中心价对称布局网格；中心价为 0 时使用中间价。
- **LevelSpacingPoints**：层级间距（点值），会按价格最小变动转换成实际价格差。
- **LevelsCount**：每个方向的层数。若使用市价模式，该参数即使大于 1 也只发送一笔市价单。
- **BuyEntryPrice / SellEntryPrice**：在非中心模式下的买入/卖出参考价，0 表示使用当前买价/卖价。
- **StopLossPrice**：绝对止损价，所有订单共用。留空时采用 `StopDistancePoints` 计算。
- **StopDistancePoints**：备用止损距离（点值），在未提供绝对止损价时启用。
- **TakeProfitDistancePoints**：可选的止盈距离（点值），为 0 时默认等于一个网格步长。
- **UseRiskSizing / RiskPercent**：开启风险百分比手数控制，并设置每个方向可承受的权益比例。程序会将该比例按层数平均分配。
- **FixedOrderVolume**：当风险计算失效或被关闭时使用的固定手数。
- **ExpirationHours**：待挂单的有效期（小时），0 表示一直有效。
- **DailyLossLimitPercent**：以权益百分比表示的当日最大亏损阈值，触发后暂停交易。
- **CloseAllPositions / CloseLongPositions / CloseShortPositions / CancelOrders**：界面可触发的手动维护命令。
- **CandleType**：用于维护与日度重置的 K 线类型。

## 实现细节
- 策略完全基于 StockSharp 的高层 API（如 `SubscribeCandles`、`SubscribeOrderBook`、`BuyLimit`、`SellStop`），无需直接操作连接器或指标对象。
- 手数换算依赖 `Security.PriceStep` 与 `Security.StepPrice`，从而把 MQL 中以点为单位的距离转换成货币风险。
- 入场单成交后通过额外的止损/止盈委托来实现保护性离场，符合 StockSharp 在订单级联方面的最佳实践。
- 日度风控在日期变更时重置，新的权益基准会在下一根维护 K 线到达时记录。若需要在同一交易日恢复，可手动切换 `EnableGrid`。
- 原脚本依赖的全局变量、紧急清仓标志及图形清理流程被替换为结构化参数与手动开关，逻辑更加清晰。

## 使用建议
1. 启动前决定采用中心式还是锚点式网格。若使用中心模式，请设置合理的 `CenterPrice`；若使用锚点模式，确保填写对应的买/卖参考价。
2. 根据品种波动调整 `LevelSpacingPoints`、`StopDistancePoints` 与 `TakeProfitDistancePoints`，这些参数均以点值表示。
3. 启用风险手数前，请确认合约配置了正确的 `PriceStep` 与 `StepPrice`，否则策略会自动退回到固定手数。
4. 修改参数或需要紧急清仓时，可先利用手动开关撤单/平仓，再调整配置。
5. 当多策略共用同一账户时，请将日度亏损限制与外部风险控制结合使用。

## 与原版 EA 的区别
- StockSharp 版本采用结构化参数和手动按钮取代了 MT4 的全局变量与注释式魔术号逻辑。
- 原脚本中的紧急关闭、网格数量自动校准以及图形清除流程在此实现中简化为显式参数和手动命令。
- 未复刻 MQL 中的跟踪止损辅助函数，如需动态止损可结合 StockSharp 的其他策略组件。
- 原版“母单-子单”依赖关系未保留，每个网格层独立运行并配备自身的保护性委托。

这些调整保留了 Colibri 网格策略“多层进场 + 严格资金管理”的核心思想，同时与 StockSharp 的最佳实践保持一致。
