# 手工构建保护订单策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表根据确认后的 EMA(14)/EMA(50) 交叉开仓或完整反转持仓，并用明确的订单生命周期方块构建保护逻辑。被接受的入场成交会固定两个保护价位。经过 100 根蜡烛的冷却期后，图表只登记一次相对入场价的止盈限价单；后续替换保持同一目标价，而止损或符合条件的反转会先发送当前止盈单的撤销请求，随后不等待撤销确认就触发市价持仓变更。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟蜡烛向快速 EMA 14 和慢速 EMA 50 提供数据，两个指标都只输出已形成的值。当快速 EMA 上穿慢速 EMA 时，Crossing 发出向上事件；下穿时发出向下事件。
- 向上交叉仅在 Position <= 0 时允许执行，向下交叉仅在 Position >= 0 时允许执行。持仓比较把空仓信号送入一张固定 Volume 的市价单。持有反向仓位时，反转路径依次触发两张相同 Volume 的市价单：第一张平掉现有方向，第二张不等待第一张成交便立即建立新方向。
- Strategy trades 的每一笔新成交都会把计数器重置为 0 并重新启动冷却期。随后恰好 100 根已完成蜡烛会阻止新入场、止盈单登记与替换以及止损检查；如果没有其他成交再次启动窗口，处理从第 101 根恢复。
- 冷却期结束后，一次性 Flag 登记固定 Volume 的止盈单。首次登记不需要订单簿就绪；订单簿相应一侧的就绪状态只允许后续由已完成蜡烛计时的替换。多头使用 Entry Price * (1 + Take Profit fraction) 的卖出限价，空头使用 Entry Price * (1 - Take Profit fraction) 的买入限价。
- Combination 只把首次登记 Order 和后续替换克隆合并为一个订单流。连接的 replaceOrder.order 与 cancelOrder.order 输入会记住最近转发的订单。订单簿分支经过有意简化：BestBid 和 BestAsk 只允许后续按已完成蜡烛计时、保持同一固定目标的替换，绝不会移动目标。止损或符合条件的反转会先请求撤销止盈单，随后不等待确认就触发市价操作。

## 入场与出场规则

- **做多入场**: 确认的 EMA 向上交叉出现、Position <= 0 且冷却期已结束时，空仓路径发送一张 Volume 的市价买单。持有空头时，图表先请求撤销空头止盈单，再触发两张相同 Volume 的市价买单：第一张平空，第二张不等待平仓成交便立即开多。Entry Price 只取自空仓开仓成交或反转中的第二张开仓成交。
- **做空入场**: 确认的 EMA 向下交叉出现、Position >= 0 且冷却期已结束时，空仓路径发送一张 Volume 的市价卖单。持有多头时，图表先请求撤销多头止盈单，再触发两张相同 Volume 的市价卖单：第一张平多，第二张不等待平仓成交便立即开空。Entry Price 只取自空仓开仓成交或反转中的第二张开仓成交。
- **离场**: 冷却期结束后，多头分支保留一张固定 Volume、价格为 Entry Price * (1 + 0.006) 的卖出限价单，空头分支保留一张固定 Volume、价格为 Entry Price * (1 - 0.006) 的买入限价单。已完成蜡烛的收盘价等于或低于 Entry Price * (1 - 0.003) 时止损多头；等于或高于 Entry Price * (1 + 0.003) 时止损空头。止损先请求撤销当前止盈单，随后不等待确认就发送一张相反方向、相同固定 Volume 的市价单。符合条件的反向交叉在两单反转前使用相同的“请求后触发”规则。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | 五分钟周期；只有已完成蜡烛才驱动 EMA 信号、冷却计数、止损检查和订单生命周期时钟。 |
| Fast EMA Length | 14 | 快速 ExponentialMovingAverage 的长度；只输出已形成的值。 |
| Slow EMA Length | 50 | 慢速 ExponentialMovingAverage 的长度；只输出已形成的值。 |
| Take Profit fraction | 0.006 | 相对 Entry Price 的有利比例。0.006 表示 0.6%，形成 1:2 的止损与止盈比例。 |
| Stop fraction | 0.003 | 相对 Entry Price 的不利比例。0.003 表示 0.3%。 |
| Volume | 1 | 每个入场单、止盈登记与替换以及市价止损使用的固定数量。反转发送两张相同 Volume 的独立市价单：先平仓，再开仓。 |
| Cooldown | 100 | 每笔 Strategy trades 成交之后被阻止的已完成蜡烛数量。每笔新成交都会把计数器重置为 0；如果没有后续成交再次启动窗口，处理从第 101 根恢复。 |

## 图表详情

- [蜡烛](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)方块只输出已完成的五分钟蜡烛。收盘价转换器和两个仅限已形成值的[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)方块提供收盘价、ExponentialMovingAverage 14 和 ExponentialMovingAverage 50。
- [交叉](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)、[持仓](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html)和[比较](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)组成对称的 Position <= 0 与 Position >= 0 入场门。
- 独立的[修改仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)方块实现空仓 OpenPosition，以及反转的 NoCondition 平仓和开仓两部分。每个方块都接收相同的固定 Volume。平仓部分先触发，但开仓部分不等待其成交便立即触发，因此异步执行可能产生竞态。Formula 方块计算冷却状态以及止盈和止损值，绝不计算订单数量。
- [策略成交](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html)只用于在每笔自身成交后重置冷却期并向图表输出成交。只有空仓开仓方块的 trade 输出和反转中第二个开仓方块的 trade 输出把已接受的 Trade.Price 送入相应的多头或空头保护状态；反转中第一个平仓成交被排除。
- [市场深度](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/market_depth.html)提供 BestBid 和 BestAsk 的就绪状态。两者都不参与止盈价公式：多头与空头目标始终锚定 Entry Price。
- 多头和空头价格公式分别连接到限价[订单登记](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/register.html)方块。Flag 在冷却结束后只允许登记一次，后续每次替换都复用同一个计算价格和相同的固定 Volume。
- 首次登记 Order 和每个替换克隆都进入 Combination<Order> 总线，该总线只负责合并和转发。连接的 replaceOrder.order 输入以及[订单撤销](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html)方块的 cancelOrder.order 输入会记住最近转发的订单。重新登记仍在进行时，撤销因此仍可能指向先前转发的订单引用。止损或反转时先发送撤销请求，随后立即触发固定 Volume 的市价止损或两张固定 Volume 的反转单，不等待撤销确认；该序列不保证原子性。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
