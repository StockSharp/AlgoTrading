# 逐笔成交费用核算策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表根据小时级 CCI(30) 对阈值的确认回归信号，以固定数量的一张市价单交易，并演示对每个已观察成交的费用核算。四根 K 线冷却期限制新信号，教学用百分比仓位保护层可以平仓，一个日志同时接收逐笔计算费用和策略引擎的累计佣金值。

![schema](schema.svg)

## 策略概览

- 已完成的一小时 K 线输入 CommodityChannelIndex 30。指标输出每个数值，包括周期尚未完全形成时产生的数值。
- 买入要求前一 CCI < -100 且当前 CCI >= -100。卖出要求前一 CCI > 100 且当前 CCI <= 100。
- 买入分支还要求 Position <= 0，卖出分支要求 Position >= 0，两个分支都要求四根 K 线冷却期已就绪。
- 每个有效信号只提交一张固定 Volume 的市价单。空仓时它开立信号方向；面对数量为一的反向仓位时，它只把仓位平到零，不会在同一信号中开立另一方向。
- 仓位保护是明确的教学层，止盈为 1%，固定止损为 0.7%。它只检查已完成 K 线的收盘价，并跳过已经产生信号订单的那根 K 线收盘价。
- 每个已观察成交按 Trade.Price × Trade.Volume × Commission Rate % / 100 计算一次模拟费用。图表显示 CCI、订单、成交及两组佣金序列，格式化后的佣金消息写入同一个日志流。

## 入场与出场规则

- **做多入场**: 当前一 CCI 低于 -100、当前 CCI 回到 -100 或更高、Position <= 0 且冷却期已就绪时，提交一张 Volume 的市价买单。空仓时开立多仓；面对数量为一的空头仓位时，只把空头仓位平到零。
- **做空入场**: 当前一 CCI 高于 100、当前 CCI 回到 100 或更低、Position >= 0 且冷却期已就绪时，提交一张 Volume 的市价卖单。空仓时开立空仓；面对数量为一的多仓时，只把多仓平到零。
- **离场**: 符合条件的反向 CCI 信号可用一张固定数量的市价单把数量为一的仓位平到零。与此独立，教学保护层可在 1% 止盈或 0.7% 固定止损处平掉其跟踪的敞口；其价格输入只接收未产生信号订单的已完成 K 线收盘价。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 01:00:00 | 一小时时间周期；只有已完成 K 线驱动 CCI 决策、冷却计数增加和保护价格检查。 |
| CCI Length | 30 | CommodityChannelIndex 的周期；方块无需等待指标完全形成便输出数值。 |
| Lower Level | -100 | CCI 下阈值。向上回穿 -100 时产生买入侧交叉条件。 |
| Upper Level | 100 | CCI 上阈值。向下回穿 100 时产生卖出侧交叉条件。 |
| Cooldown | 4 | 一次成交后，再次允许信号交易所需的已完成 K 线数量。 |
| Commission Rate % | 0.04 | 仅供逐笔显示公式 `Trade.Price × Trade.Volume × rate / 100` 使用的百分比费率。 |
| Take Profit % | 1 | 教学仓位保护层采用的有利方向百分比变动。 |
| Stop Loss % | 0.7 | 教学保护层中固定、非跟踪止损采用的不利方向百分比变动。 |
| Volume | 1 | 每张买入或卖出信号订单的固定数量。 |

## 图表详情

- [K 线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)方块输出已完成的一小时 K 线。其收盘价会为保护层保存，[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)方块则在关闭已形成值过滤后计算 CCI 30。逐 K 线标志会保留前一值和当前值与阈值的四项比较结果，直到最终决策脉冲。
- [当前仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html)提供 Position <= 0 和 Position >= 0 门控。冷却计数从 4 开始，在每根 K 线决策前递增并封顶；每次信号订单直接成交及保护成交都会将其重置为 0。因此成交后的第 1、2、3 根已完成 K 线被阻止，第 4 根可以交易。
- 两个[订单注册](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/register.html)方块提交固定 Volume 的市价买单和卖单。每个方块的直接成交输出更新保护并重置冷却；专用 Trades for order 方块观察已注册的 Order，并向模拟费用计算和图表提供信号成交流。
- 两个直接信号成交流都进入[仓位保护](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)，使其内部仓位在信号平仓后回到零。保护自身的成交输出不会反馈到该输入。无信号门控仅在该 K 线上两类信号订单都未触发时，才释放保存的已完成收盘价供保护检查。
- 对于每个已观察的买入、卖出或保护成交，转换器将 Trade.Price 和 Trade.Volume 保存到静默锁存器。释放脉冲随后按费率、价格、数量的顺序输出；[公式](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html)更新 `a × b × r / 100`，最后静默费用状态只为该成交输出一次。
- [策略盈亏](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html)的 Commission 输出是引擎累计佣金；如果测试或实盘环境没有配置佣金规则，它会保持为零。逐笔模拟公式只用于显示，不会写入该引擎值。
- 两个[字符串格式化](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html)输出由 Combination<IComparable> 合并，并发送到一个 Log 类型的[通知](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)。Trades for order 收到注册 Order 后才订阅，因此若执行环境在注册调用内部完成订单，成交可能早于观察器连接；此时直接成交输出仍会驱动保护和冷却。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
