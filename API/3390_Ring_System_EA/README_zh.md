# Ring System EA 策略

该策略将 MetaTrader 4 平台上的多货币网格对冲专家 "RingSystemEA" 移植到 StockSharp 的高级 API。程序会根据设定的货币顺序自动生成所有三币组合，形成三条互相关联的货币对，并为每个组合同时维护 **正向篮子**（买/卖/买）和 **反向篮子**（卖/买/卖）。策略持续跟踪每个篮子的浮动盈亏，按照阶梯规则在亏损扩大时加仓，并在达到预设盈利或止损目标时成组平仓。

## 交易逻辑

* 从 `CurrenciesTrade` 字符串解析货币序列，对每三个货币组合生成三只货币对（例如 EUR/GBP/AUD -> EURGBP、EURAUD、GBPAUD）。
* 每个组合同时维护两个篮子：
  * **Plus** 篮子在第一、三只货币对做多，在第二只货币对做空。
  * **Minus** 篮子执行相反的卖/买/卖结构。
* 当全部价格到位且交易时段允许时自动建立篮子，可通过 `SideOpenOrders` 指定只交易其中一边。
* 若正在运行的篮子亏损超过 `StepOpenNextOrders` 阈值（阈值可按 `StepOrdersProgress` 递增），则按照 `LotOrdersProgress` 规则加仓，形成类似马丁格尔的层级结构。
* 盈利退出方式由 `TypeCloseInProfit` 控制，可分别关闭单个篮子、整个组合或逐一关闭货币对。
* 亏损处理由 `TypeCloseInLoss` 决定，可完全平仓、减半仓位或允许篮子继续运行。
* 可选的会话控制实现了 MT4 版本中的“周一开盘等待”和“周五提前收盘”规则。
* 自动手数依据当前投资组合价值与 `RiskFactor` 计算，`UseFairLotSize` 会根据每个货币对的 Tick 价值进行调整。

## 重要参数

| 参数 | 说明 |
| --- | --- |
| `CurrenciesTrade` | 构建货币圈的基础货币顺序。 |
| `NoOfGroupToSkip` | 需要跳过的圈编号。 |
| `SideOpenOrders` | 选择仅交易 Plus、仅交易 Minus，或同时交易两侧。 |
| `OpenOrdersInLoss` & `StepOpenNextOrders` | 亏损加仓的触发逻辑与阈值。 |
| `StepOrdersProgress` | 亏损阈值随层级的增长方式。 |
| `LotOrdersProgress` | 加仓手数的递进方式。 |
| `TypeCloseInProfit` / `TargetCloseProfit` | 盈利退出模式与目标。 |
| `TypeCloseInLoss` / `TargetCloseLoss` | 亏损退出模式与阈值。 |
| `AutoLotSize`, `RiskFactor`, `ManualLotSize`, `UseFairLotSize` | 资金管理设置。 |
| `ControlSession`, `WaitAfterOpen`, `StopBeforeClose` | 交易时段控制。 |
| `MaxSpread`, `MaximumOrders`, `MaxSlippage` | 风险约束。 |

## 实现细节

* 核心逻辑保持与原 EA 一致：维护两组对冲篮子、在亏损时阶梯加仓、在盈利或风险触发时整体退出。
* 策略完全基于行情与 PnL 事件，不使用任何指标计算。
* 所有订单都会附加 `StringOrdersEA` 字符串，方便外部统计或比对。
* 原版的图形界面与文件输出未移植，`SaveInformations` 启用时会向策略日志写入详细的篮子状态。
* `Open_With_Auto_Step` 模式复用手动步长，因为 MT4 特有的保证金/步长估算在 StockSharp 中不可用。

## 使用建议

1. 在连接器中提前加载所有需要的货币对，必要时通过 `SymbolPrefix` / `SymbolSuffix` 适配券商代码。
2. 根据账户风险偏好设置 `SideOpenOrders`、`StepOpenNextOrders`、`StepOrdersProgress` 与 `LotOrdersProgress`，这些参数决定网格加仓的节奏与规模。
3. 若需要了解策略的具体行为，可启用 `SaveInformations` 并查看日志中的环形统计、加仓与平仓记录。

该移植版本在保留 RingSystemEA 核心对冲网格理念的同时，充分利用 StockSharp 的事件驱动架构与参数系统，方便在 .NET 生态中继续开发与扩展。
