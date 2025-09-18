# Build Your Grid 策略

本目录包含 MetaTrader 专家顾问 **BuildYourGridEA v1.8** 的 StockSharp 高级 API 版本。策略按照原始 EA 的思路在买入和卖出两个方向上构建网格，自动调整加仓距离、手数递增方式以及资金保护参数。

## 核心流程

1. **初始建仓**：根据 `OrderPlacement` 参数，先打开允许方向的首单。
2. **扩展网格**：当价格偏离上一个入场价达到设定步长时再开新单。步长可以保持固定、按订单数量线性放大，或每次翻倍。
3. **仓位管理**：手数可以始终沿用首单、按几何方式增加，或每次翻倍。启用自动手数时，将使用与 MQL 版本相同的风险系数公式计算交易量。
4. **风险控制**：当浮动盈亏达到指定的点数或货币金额时平掉全部单子；当浮亏达到阈值时可以平掉首单或所有单；也可在回撤超过账户余额百分比后触发对冲单，使净敞口回到平衡。
5. **点差过滤**：订阅 `SubscribeLevel1()` 后跟踪最佳买一/卖一报价，只要点差超过 `MaxSpread`（单位：点），就禁止开新仓。

## 实现说明

- 仅使用高级 API（`SubscribeCandles`、`SubscribeLevel1`、`BuyMarket`、`SellMarket`）。
- 通过两个列表跟踪所有买、卖仓位的入场价与手数，在 `OnNewMyTrade` 中同步更新，模拟 MetaTrader 票据信息。
- 对冲模式在净额账户中表现为“削减反向持仓”。触发对冲时，策略按多空手数差乘以 `MuliplierHedgeLot` 下单，效果等同于原 EA。
- 点值和货币换算优先使用品种的 `PriceStep` 与 `StepPrice`，若数据缺失则采用安全默认值，保证回测可运行。

## 参数概览

| 参数 | 说明 |
| --- | --- |
| `CandleType` | 用于生成信号的主周期。 |
| `OrderPlacement` | 允许交易的方向（双向 / 仅多 / 仅空）。 |
| `NextOrder` | 新单顺势还是逆势加仓。 |
| `PipsForNextOrder` | 网格基础步长（点）。 |
| `StepMode` | 步长进阶方式。 |
| `ProfitTarget` | 盈利目标的计算方式（点或货币）。 |
| `PipsCloseInProfit` | 当 `ProfitTarget = TargetInPips` 时的目标点数。 |
| `CurrencyCloseInProfit` | 当 `ProfitTarget = TargetInCurrency` 时的目标金额。 |
| `LossMode` | 浮亏达到阈值后的处理方式。 |
| `PipsForCloseInLoss` | 触发浮亏处理的点数阈值。 |
| `PlaceHedgeOrder` | 是否启用对冲。 |
| `LevelLossForHedge` | 触发对冲的回撤百分比。 |
| `MuliplierHedgeLot` | 对冲单手数乘数。 |
| `AutoLotSize` | 是否启用自动手数。 |
| `RiskFactor` | 自动手数使用的风险系数（按余额的百分比）。 |
| `ManualLotSize` | 自动手数关闭时的首单手数。 |
| `LotProgression` | 手数递增方式。 |
| `MaxMultiplierLot` | 首单手数可放大的最大倍数。 |
| `MaxOrders` | 同时打开的最大订单数（0 表示不限）。 |
| `MaxSpread` | 允许的最大点差（点）。 |

## 与原始 EA 的差异

- StockSharp 采用净额账户模式，因此对冲单会抵消对向仓位，而不是持有独立的多空订单，但最终结果与 MQL 版本一致。
- MQL 中的图形绘制与声音提示未迁移到该版本。
- 点差检查依赖 level1 报价；未订阅报价时会自动放宽过滤。

## 使用步骤

1. 将策略绑定到包含 candles 与 level1 报价的证券和投资组合。
2. 根据需求调整各项参数。
3. 启动策略后，它会自动管理网格、加仓与风控流程。

