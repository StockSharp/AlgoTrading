# Close All MT5 策略

**Close All MT5** 策略复刻了 MQL 实用工具的功能：当浮动盈利达到目标时自动平仓。
策略持续监控投资组合，根据所选单位（资金、点数或点值）计算浮盈，并在阈值
被突破时平掉多头或空头。手动触发器模拟原始脚本中的图表按钮，可立即关闭
符合条件的持仓并撤销挂单。

## 工作流程

* 订阅所有存在持仓品种的逐笔数据。
* 保存最新成交价并与每个持仓的平均建仓价比较。
* 根据所选单位（资金、点、pip）计算浮动盈亏。
* 当浮盈超过设定值（或浮亏低于设定值）时发送市价单关闭相应持仓。
* 提供手动触发器，可按模式（全部、多头、空头、按合约等）立即平仓。

## 参数

| 名称 | 说明 |
| ---- | ---- |
| `Position Filter` | 指定自动检查时关注多头、空头或两者。 |
| `Profit Mode` | 浮动盈亏的计量单位（`Money`、`Points`、`Pips`）。 |
| `Profit Threshold` | 触发平仓的目标浮盈（正数）或浮亏（负数）。 |
| `Close Mode` | 手动平仓模式，对应原脚本的按钮行为。 |
| `Comment Filter` | 只有当持仓的策略标识包含该子串时才会被处理。 |
| `Currency Filter` | `CloseCurrency` 模式使用的合约标识，留空时采用当前策略品种。 |
| `Magic Number` | `CloseMagic` 模式使用的策略编号（相当于 magic number）。 |
| `Ticket Number` | `CloseTicket` 模式要处理的具体持仓编号。 |
| `Max Slippage` | 允许的最大滑点（价格步长），暂保留以供后续扩展。 |
| `Trigger Close` | 设为 `true` 立即执行手动平仓，完成后自动恢复为 `false`。 |

## 说明

* 优先使用投资组合提供的浮动盈亏数据，若不可用则按价格步长自行计算。
* `Comment Filter` 和 `Magic Number` 与持仓关联的策略标识匹配，对应 MQL 脚本中的
  注释与 magic number 过滤。
* 当 `Close Mode` 为 `CloseAllAndPending` 时，策略会在平仓后撤销注释匹配的挂单。
