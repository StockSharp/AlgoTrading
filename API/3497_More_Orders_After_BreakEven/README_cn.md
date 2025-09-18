# More Orders After BreakEven（StockSharp移植版）

本目录提供 MetaTrader 4 智能交易系统 **“More Orders After BreakEven”**（MQL 源编号 `35609`）的 C# / StockSharp 实现。原策略在已有仓位被移动到保本之后继续加仓多单。本移植版保留了按单管理的特性，并使用 StockSharp 的高级 API 完成下单、监控与出场。

## 策略概要

* **交易方向**：仅做多。每次交易都会向主标的提交市价买入。
* **核心思想**：只要尚未进入保本状态的订单数量少于 `MaximumOrders`，就继续买入。当某笔交易达到 `BreakEvenPips` 指定的盈利距离时，其止损会上移到开仓价，从而不再限制后续加仓。
* **出场方式**：每个订单维护独立的止损与止盈。价格触发保本后止损会被提升到入场价；当买价触碰止盈或止损时，策略会以市价卖出剩余仓位。
* **行情处理**：原始 MQL 代码基于 `OnTick`。移植版通过订阅 Level1（最优买卖价）数据来模拟同样的逐笔逻辑。

## 参数说明

| 参数 | 含义 | 默认值 |
|------|------|--------|
| `MaximumOrders` | 尚未保本的多单数量上限。数量低于该值时允许继续开仓。 | `1` |
| `TakeProfitPips` | 入场价到止盈价的距离（MetaTrader 点）。为 `0` 表示不开启止盈。 | `100` |
| `StopLossPips` | 初始止损距离（MetaTrader 点）。为 `0` 表示无初始止损。 | `200` |
| `BreakEvenPips` | 触发保本的盈利距离（MetaTrader 点）。为 `0` 表示只要价格略高于入场价就移动到保本。 | `10` |
| `TradeVolume` | 每次市价买入的手数。 | `0.01` |
| `DebugMode` | 是否输出与 MetaTrader `Comment()` 类似的调试日志。 | `true` |

所有基于“点”的距离都会根据品种的小数位数自动调整，以重现原策略中 `points` 变量对 4/2 和 5/3 位报价的处理。

## 运行流程

1. **订阅 Level1 行情**：当最优买价与最优卖价均可用时调用 `ProcessPrices`，对应 MQL 的 `OnTick` 循环。
2. **统计有效订单**：计算尚未移动到保本的订单数量，对应原脚本的 `OrdersCounter()`。
3. **开仓**：若数量小于 `MaximumOrders`，以 `TradeVolume` 的手数买入。成交后记录入场价、止损与止盈水平。
4. **移动止损到保本**：一旦买价超过 `Entry + BreakEvenPips`，将止损设置为入场价，并标记该单已进入保本。
5. **离场检测**：买价达到止盈或跌破止损（包括保本止损）时，发送市价卖出指令关闭对应仓位。
6. **仓位跟踪**：通过 `OnOwnTradeReceived` 维护一个 FIFO 列表，模拟 MetaTrader 逐单管理的行为，即便 StockSharp 以净头寸的方式展示账户状态。

## 与原版的差异

* 仅实现多头开仓，因为原始 EA 并不会做空。
* 在 StockSharp 高级 API 中，个别订单的止损/止盈需要由策略自行监控并以市价单执行，不会像 MetaTrader 那样修改服务器端订单属性。
* 调试输出通过 StockSharp 的日志系统实现，而不是在图表上调用 `Comment()`。

## 使用建议

1. 将策略连接到能够提供 Level1 数据的行情源或交易通道。
2. 根据交易品种的波动性和券商限制调整各项“点”参数。
3. 在测试阶段启用 `DebugMode` 观察加仓与保本行为，确认无误后可关闭以减少日志量。
4. 注意账户可用资金，确保能够承受在多笔订单进入保本后继续加仓的需求。

## 相关文件

* 原始 MQL4 文件：`MQL/35609/More Orders After BreakEven.mq4`。
* 转换后的 C# 策略：`CS/MoreOrdersAfterBreakEvenStrategy.cs`。
