# Money Manager 策略

## 概述
MetaTrader 5 专家顾问 “Money manager 1.0” 不会自主开仓，而是对现有持仓执行资金管理：当浮动盈利达到账户余额的一定百分比（外加每手佣金和当前点差），或浮动亏损超过允许的百分比时，立即平仓。它是叠加在其他策略之上的保护层。

移植到 StockSharp 后，策略保持完全相同的行为。它通过 `SubscribeLevel1()` 订阅 Level1 行情，实时记录最新的买一/卖一价格，并在每次更新时评估合并头寸的未实现盈亏。当利润超过 `ProfitDeal` 阈值（含费用补偿）时调用 `ClosePosition()`；如果亏损低于 `LossDeal` 下限，同样会平仓。两个规则都可以单独关闭，与原始 MQL 程序一致。

## 迁移说明
- `AccountInfoDouble(ACCOUNT_BALANCE)` 被替换为 `Portfolio.CurrentValue`，这是 StockSharp 中与账户余额最接近的指标，用来计算百分比阈值。
- 通过 Level1 报价重建 `SymbolInfoDouble(_Symbol, SYMBOL_BID/ASK)` 的行为，并根据买卖价差估算交易成本。
- `POSITION_PROFIT` 的逻辑通过 `PositionPrice`（平均持仓价）以及根据仓位方向选择的最佳买/卖价复现，从而保持多空两种情况下的盈亏符号。
- 所有参数都封装为 `StrategyParam<T>`，可以在 StockSharp 图形界面中修改或用于优化。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| ---- | ---- | ------ | ---- |
| `ProfitDealEnabled` | `bool` | `true` | 是否启用基于利润的自动平仓。 |
| `LossDealEnabled` | `bool` | `true` | 是否启用亏损保护。 |
| `ProfitPercent` | `decimal` | `5` | 浮动盈利达到账户余额的百分之多少后锁定收益。 |
| `LossPercent` | `decimal` | `5` | 允许的最大亏损，占当前余额的百分比。 |
| `LotCommission` | `decimal` | `7` | 每手佣金，计入利润和亏损阈值。 |
| `LotSize` | `decimal` | `0.1` | 用于将佣金和点差换算为金额的参考手数。 |

## 使用步骤
1. 为策略指定要管理的证券和投资组合，相关持仓可由其他系统开立。
2. 根据经纪商规则配置 `LotCommission` 和 `LotSize`，以便正确估算成本。
3. 调整 `ProfitPercent` 与 `LossPercent`，必要时可以通过优化器寻找最优组合。
4. 启动策略。它会持续监听 Level1 行情，一旦满足任一阈值，就会关闭持仓并停止风险暴露。

该移植版不会改变下单逻辑或手数，只负责按照原始 MetaTrader 顾问的设定来保护已有仓位。
