# ROC 策略

## 概述
ROC 策略是在 StockSharp 高级 API 中对 MetaTrader 专家顾问 `MQL/26938/ROC.mq4` 的移植。该策略只针对单一品种运行，通过一组线性加权移动平均线（LWMA）、自定义速度变化模型（ROC）、更高周期的动量指标以及月度 MACD 来判断趋势。原版中的资金管理模块全部保留，包括保本、按点数移动的追踪止损、权益保护以及以货币或百分比计的止盈。

## 入场逻辑
1. 订阅三个数据源：交易周期、用于 14 周期 Momentum 的更高时间框架以及月线 MACD。
2. 每根完成的交易周期蜡烛会检查以下条件：
   - 自定义 ROC 模型对多头返回上升趋势 (`Line4 < Line5`)，对空头返回下降趋势 (`Line4 > Line5`)。
   - 快速 LWMA 必须在慢速 LWMA 之上才能做多，在其之下才能做空。
   - 较高周期 Momentum 的最近三个读数中至少有一个与 100 的偏差大于对应阈值。
   - 月线 MACD 主线位于信号线之上（做多）或之下（做空）。
   - 未超过 `MaxTrades` 限制的分批次数，并且在 `IncreaseFactor` 大于零时可以在连续亏损后提高下一笔交易量。

## 出场逻辑
- 当仓位发生变化时，根据 MetaTrader 点数计算初始止损和止盈。
- 如果开启 `UseBreakEven`，在达到触发距离后将止损移至入场价加上偏移量。
- `TrailingStopSteps` 会在每根蜡烛收盘时收紧止损。
- `UseTpInMoney`、`UseTpInPercent` 与 `EnableMoneyTrailing` 控制的资金管理模块会在达成货币或百分比目标时离场，并且在浮动盈利回撤超过 `StopLossMoney` 时获利了结。
- `UseEquityStop` 会把当前权益与历史高点比较，若回撤超过 `TotalEquityRisk`，即刻平仓。
- 将 `ExitStrategy` 设为 `true` 可强制立即平仓。

## 参数
| 名称 | 说明 |
| --- | --- |
| `LotSize` | 基础下单手数。 |
| `IncreaseFactor` | 连续亏损后调整下一笔手数。 |
| `FastMaPeriod` / `SlowMaPeriod` | LWMA 趋势过滤参数。 |
| `PeriodMa0`, `PeriodMa1`, `BarsV`, `AverBars`, `KCoefficient` | 自定义 ROC 模型参数。 |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | 高周期 Momentum 的绝对偏差阈值。 |
| `StopLossSteps`, `TakeProfitSteps` | 初始止损与止盈的点数。 |
| `TrailingStopSteps` | 经典点差追踪止损。 |
| `UseBreakEven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | 保本模块设置。 |
| `UseTpInMoney`, `TpInMoney`, `UseTpInPercent`, `TpInPercent` | 货币与百分比止盈设置。 |
| `EnableMoneyTrailing`, `TakeProfitMoney`, `StopLossMoney` | 资金追踪止盈参数。 |
| `UseEquityStop`, `TotalEquityRisk` | 权益保护参数。 |
| `MaxTrades` | 每个方向允许的最大加仓次数。 |
| `ExitStrategy` | 启用后立即平仓。 |

## 说明
- Momentum 的时间框架会根据交易周期自动匹配，以复现原始 MQL 中的 `switch` 逻辑。
- 全部指标都通过 `Bind` 使用，不需要手动请求历史数据。
- 策略按净头寸模式运行：当出现做多信号而当前持有空单时，会先平掉空单再进场做多，模拟非对冲账户上的工作方式。
