# Elite eFibo Trader 策略

## 概述
Elite eFibo Trader 复刻了经典的马丁网格专家顾问：在趋势过滤条件满足时，以斐波那契序列的手数逐级加仓。该 StockSharp 版本保留了原始的分批建仓结构——首单以市价成交，其余订单按照设定的点距挂入止损单；每当上一级被触发后，下一层继续排队。策略在浮动盈亏达到现金目标或趋势过滤器转向时关闭整篮仓位。

## 市场数据
- 仅订阅一个可配置的蜡烛类型（默认 15 分钟蜡烛）。
- 指标与止损判断均基于蜡烛收盘价。

## 入场逻辑
1. 方向由均线交叉逻辑（默认开启）或手动开关 `ManualOpenBuy`/`ManualOpenSell` 决定。
2. 当启用均线逻辑时，快速均线向上穿越慢速均线会激活做多阶梯，反向穿越激活做空阶梯；每根蜡烛只触发一次信号。
3. 启用 RSI 过滤后，仅当 `RSI > RsiHigh` 才允许建立多头篮子，`RSI < RsiLow` 才允许空头篮子。
4. 只有在策略没有任何未完成订单或持仓且允许继续交易（`TradeAgainAfterProfit`）时才会开启新的阶梯。
5. 第一层使用市价单，其余层按照 `LevelDistancePips` 的点距挂出止损单；各层手数默认为斐波那契数列，可逐层调整。

## 出场逻辑
- 每个已成交的层级都会获得基于 `StopLossPips` 的初始止损，并在均线给出反向信号时参与拖移。
- 多头篮子的止损会跟随到 `收盘价 - TrailingStopPips`，空头篮子跟随到 `收盘价 + TrailingStopPips`，且永远不会远离当前止损。
- 当蜡烛的最高/最低触及某一层的止损价时，该层剩余仓位以市价平仓。
- 若整篮浮动盈亏（基于 `PriceStep` 与 `StepPrice` 计算）达到 `MoneyTakeProfit`，策略立即平掉所有仓位并撤销未成交的挂单。
- 当篮子完全出清后会自动撤销全部止损单；若 `TradeAgainAfterProfit` 设为 `false`，策略会保持空闲直至重新启动。

## 参数
| 参数 | 说明 |
| ---- | ---- |
| `UseMaLogic` | 启用/关闭均线交叉方向判定。 |
| `MaSlowPeriod`, `MaFastPeriod` | 慢速与快速 SMA 的周期。 |
| `TrailingStopPips` | 均线反向时使用的拖移止损距离（点）。 |
| `UseRsiFilter`, `RsiPeriod`, `RsiHigh`, `RsiLow` | RSI 过滤器设置。仅当 RSI 高于 `RsiHigh` 才能做多，低于 `RsiLow` 才能做空。 |
| `ManualOpenBuy`, `ManualOpenSell` | 关闭均线逻辑后用于手动控制方向的开关。 |
| `TradeAgainAfterProfit` | 达到现金止盈后是否重新启动新的篮子。 |
| `LevelDistancePips` | 各层挂单之间的点距。 |
| `StopLossPips` | 每层的初始止损距离。 |
| `MoneyTakeProfit` | 以账户货币计价的篮子止盈目标。 |
| `Level1Volume` … `Level14Volume` | 各层手数；设为 0 可禁用该层。 |
| `CandleType` | 指标计算所用的蜡烛类型/周期。 |

## 实现细节
- 点值换算与 MQL 版本一致：若品种的 `Decimals` 为 3 或 5，则使用 `PriceStep * 10` 作为 1 点，对应原策略中的 `MyPoint` 处理。
- 每个层级单独跟踪其入场价、剩余仓位与止损价，因此能够复现部分成交及单独止损的行为。
- 浮动盈亏通过 `PriceStep` 与 `StepPrice` 计算，请确认品种的这两个属性已正确配置，否则现金止盈无法触发。
- 启动时调用一次 `StartProtection()` 以启用 StockSharp 的保护机制。
- 当没有任何持仓时会自动执行撤单，重现原脚本多次调用 `subCloseAllPending()` 的做法。

## 使用建议
- 检查交易品种的 `PriceStep`、`StepPrice`、`VolumeStep` 以及最小/最大交易量，确保斐波那契手数合法。
- 请选择与 MetaTrader 原策略一致的蜡烛周期，以避免信号偏移。
- 马丁策略在单边行情中会迅速累积仓位，请先在模拟或历史数据上验证。
- 关闭 `UseMaLogic` 可手动控制方向，保持开启则由均线自动判断趋势。
