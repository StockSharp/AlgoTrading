# Trailing Only Close All Button 策略

该策略复刻 MetaTrader 专家顾问 *Trailing Only with Close All Button*。策略本身不会开仓，只负责管理其他逻辑或手工交易产生的持仓：根据 MetaTrader 式的点值参数设置止损、止盈与跟踪止损，并提供一个手动的 **Close All** 按钮，可按指定过滤条件批量平仓或撤单。

## 主要特性

- 订阅 **Level1** 行情以获取最新的买一/卖一报价，用于计算保护价位。
- 持仓出现后立即依据 `StopLossPips`、`TakeProfitPips` 生成初始止损和止盈价格。
- 当浮动盈利超过 `TrailingStopPips + TrailingStepPips` 时启动跟踪止损，每当价格再前进 `TrailingStepPips` 点，就把止损向市场方向推进。
- 市价触发计算出的止损或止盈时，通过 `SellMarket` / `BuyMarket` 市价单平仓，行为等同于 MetaTrader 在服务器侧执行保护单。
- `CloseAll` 按钮可以只平仓、只撤单或同时执行，并可附加交易品种与浮动盈亏过滤条件，忠实还原原始脚本的功能。

## 参数说明

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `StopLossPips` | 入场价到止损位的距离，单位为 MetaTrader 点（5 位外汇报价为 0.00010）。 | 500 |
| `TakeProfitPips` | 入场价到止盈位的距离，单位为 MetaTrader 点。 | 1000 |
| `TrailingStopPips` | 启动跟踪止损所需的最小盈利（MetaTrader 点）。 | 200 |
| `TrailingStepPips` | 每次移动止损前需要额外满足的盈利幅度（MetaTrader 点）。启用跟踪止损时必须大于 0。 | 50 |
| `ManualCloseMode` | 按钮的作用范围：仅平仓、仅撤单或两者一起。 | Positions |
| `ManualCloseSymbol` | 限制按钮只作用于当前品种，或扩展到策略涉及的全部品种。 | Chart |
| `ManualCloseProfitFilter` | 手动平仓时的浮盈浮亏过滤：全部平仓、只平盈利单或只平亏损单。 | ProfitOnly |
| `CloseAll` | 虚拟按钮。在界面中设置为 `true` 即触发手动关闭流程，策略会自动重置为 `false`。 | `false` |

## 实现细节

- 点值转换基于品种的 `PriceStep`，若价格精度为 3 或 5 位小数，则乘以 10 以匹配 MetaTrader 对“点”的定义。
- 全程使用高级 API：`SubscribeLevel1()` 接收行情，`BuyMarket` / `SellMarket` 发出市价指令。
- 当净持仓归零时会清空所有跟踪与手动关闭的内部状态，避免旧数据影响下一笔交易。
- 手动关闭流程遍历 `Portfolio.Positions` 以及策略维护的订单列表，在执行平仓或撤单前套用配置的过滤条件。
