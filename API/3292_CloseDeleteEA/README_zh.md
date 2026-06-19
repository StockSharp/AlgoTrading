# CloseDeleteEA 策略

## 概述
CloseDeleteEA 策略用于批量平仓并撤销挂单。
策略会按设定的时间间隔扫描投资组合，根据过滤条件发送市价单或撤单指令。
当需要快速退出市场、手工操作过于缓慢时，这个工具非常有帮助。

## 主要特性
- 通过市价单关闭多头或空头敞口。
- 撤销符合过滤条件的挂单。
- 通过盈亏过滤避免处理特定仓位。
- 可选择仅处理当前标的或扫描整个投资组合。
- 按策略标识过滤，类似于 MetaTrader 的 magic number。

## 参数
| 名称 | 说明 |
| --- | --- |
| `CloseBuyPositions` | 关闭满足过滤条件的多头敞口。 |
| `CloseSellPositions` | 关闭满足过滤条件的空头敞口。 |
| `CloseMarketPositions` | 启用平仓模块。 |
| `CancelPendingOrders` | 启用挂单撤销模块。 |
| `CloseOnlyProfitable` | 仅在当前盈亏不为负时平仓。 |
| `CloseOnlyLosing` | 仅在当前盈亏不为正时平仓。 |
| `ApplyToCurrentSecurity` | 为 `true` 时仅扫描策略标的，为 `false` 时扫描整个投资组合。 |
| `TargetStrategyId` | 按策略标识过滤（留空表示全部匹配）。 |
| `TimerInterval` | 管理循环的执行周期。 |

## 使用步骤
1. 将策略连接到包含目标投资组合的终端。
2. 根据需求调整过滤参数。
3. 启动策略以触发批量平仓/撤单流程；当所有匹配对象处理完毕后策略会自动停止。
4. 需要注意，撤单请求只针对策略在连接器中可见的挂单。

## 与 MQL 版本的差异
- StockSharp 使用合并仓位，因此以净头寸体量代替逐笔订单控制。
- 策略标识过滤用于模拟原策略中的 magic number 行为。
- MetaTrader 中的图形界面元素（背景图层等）未被移植。
