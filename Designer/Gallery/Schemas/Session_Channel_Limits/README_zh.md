# 时段通道限价单策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图每天固定一次九小时价格通道快照，并在上下边界放置一组由客户端管理的 OCO 挂单。订单成交后由图中逻辑请求撤销另一张订单，并非交易所的原子 OCO 指令。已完成的五分钟 K 线定义通道，实时 BestBid 订阅提供成交所需的报价事件；下一次每日重置会撤销剩余订单，再用 Order Volume 数量的 ReduceOnly 市价动作调整持仓。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟 K 线进入仅输出已形成值的 Highest 108 和 Lowest 108。每日快照时，滚动窗口包含 OpenTime 从 UTC 01:00 到 09:55 的 K 线，即完整的 01:00–10:00 时段。
- 下单工作时间模块选择时间戳位于 09:55:00–09:59:59 的已完成 K 线。K 线时间戳采用 OpenTime，因此该 K 线在 UTC 10:00 完成时才送达；Flag 把窗口结果转换为每个时段恰好一个下单脉冲。
- 该脉冲锁定通道的两条边界和当前持仓。当 Session Low < Session High 且 Position = 0 时，图先在已锁定低点注册买入限价单，再在已锁定高点注册卖出限价单；两者数量均为 1，并关闭 ShrinkPrice。
- 持续订阅的 Level1 模块读取 BestBid。其报价流让交易连接器持续获得实时价格更新，以便两个挂单在市场触及其价格时成交；BestBid 不会替换任何已锁定的通道边界。
- 任一限价单产生第一笔 MyTrade 后，都会请求撤销已保存的反向 Order。这是客户端 OCO 逻辑：在正常的顺序事件处理中，它旨在只留下一张已成交通道订单；由于撤单并非交易所原子操作，近乎同时的成交仍可能发生竞态。下一次重置先发出批量撤单请求，再把两个已保存的 Order 引用送入各自的确定性撤单路径，并以 Order Volume 数量提交 ReduceOnly 市价动作。在正常的单笔成交情形下，该数量会完整平仓；如果实际敞口不同，ReduceOnly 只会减仓。图表显示 K 线、两条通道线、两个订单流，以及入场和重置平仓的全部成交。

## 入场与出场规则

- **做多入场**: 在 UTC 10:00 的时段边界，如果两个 108 值指标均已形成、已锁定低点小于已锁定高点且持仓快照为零，图就在 Session Low 以 Order Volume 放置买入限价单。该订单保持有效，直到成交或被任一撤单路径移除。
- **做空入场**: 在相同的通道形成和空仓检查下，图在 Session High 以 Order Volume 放置卖出限价单。如果该订单先成交，其 MyTrade 事件会把已保存的买入限价单送入撤单模块。
- **离场**: 图中没有止损或止盈模块。通道订单首次成交后，客户端逻辑请求撤销反向订单，而不是主动反转持仓。持仓保持到与 00:55 K 线关联的重置，并在该 K 线于 UTC 01:00 完成时处理；重置请求批量撤单、明确撤销两个已保存限价单，再使用 Order Volume 数量的 ReduceOnly 市价动作。它会平掉正常单笔成交产生的持仓；若实际敞口不同，也不会增加敞口或反向开仓。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles Series | 00:05:00 | 五分钟 K 线序列；只有已完成 K 线才更新通道并驱动两个每日时间窗口。 |
| Session High Length | 108 | 仅输出已形成值的 Highest 所使用的已完成 K 线数量。在默认周期和窗口下，108 根覆盖 UTC 01:00–10:00。 |
| Session High Source | unset | 未设置。Highest 自动读取每根已完成 K 线的 High。 |
| Session Low Length | 108 | 仅输出已形成值的 Lowest 所使用的已完成 K 线数量。应与 Session High Length 保持一致，使两条边界描述同一时段。 |
| Session Low Source | unset | 未设置。Lowest 自动读取每根已完成 K 线的 Low。 |
| Order Volume | 1 | 每张挂起限价单和重置时 ReduceOnly 动作使用的数量。在正常单笔成交路径中，它与形成的持仓数量相同；若实际数量不同，ReduceOnly 可防止重置动作增加敞口或反向开仓。 |

## 图表详情

- [K 线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)模块只输出已完成的五分钟 K 线。两个仅输出已形成值的[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)模块直接根据该数据流计算滚动 Highest 108 和 Lowest 108。
- 两个[工作时间](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)模块检查 K 线 OpenTime。09:55:00–09:59:59 下单区间在该 K 线于 UTC 10:00 完成时执行，00:55:00–00:59:59 区间则在 UTC 01:00 完成时执行。这个单根 K 线偏移属于时间配置，并非执行延迟。
- 下单 [Flag](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/flag.html)只输出一次并保持设置状态直到重置。[变量](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)模块在该脉冲上锁定 Highest、Lowest、Position、零和数量；[比较](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)与[逻辑条件](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)只允许一组通道有效且持仓为零的订单。
- 买入[订单注册](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/register.html)模块先在 Session Low 放置限价单。其注册后的 Order 在触发 Session High 的卖出注册之前刷新高点和数量输入。两张订单均设置 ShrinkPrice=false，并保持工作状态直到成交或撤销。
- [Level1](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html)模块持续订阅 BestBid。保留的报价流启用用于挂单撮合的实时报价处理，而订单价格仍完全来自锁定的 Highest 和 Lowest。
- 每张注册后的 Order 都会保存。买单 MyTrade 把已保存卖单交给[订单撤销](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html)，卖单 MyTrade 则对买单执行对称操作。重置调用[批量撤单](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html)，随后也把两个已保存的 Order 引用送入各自的撤单模块，因此清理不依赖批量请求确认。
- 最后，重置脉冲以 ReduceOnly 条件、Order Volume 数量和 MarketOrder 算法触发[修改持仓](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)。在预期的顺序单笔成交路径中，该数量会完整平仓；如果成交发生竞态或实际敞口不同，ReduceOnly 可防止动作增加敞口或反向开仓。[图表面板](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/chart.html)接收已完成 K 线、Highest、Lowest、两个 Order 流、两个限价 MyTrade 流以及重置平仓 MyTrade 流。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
