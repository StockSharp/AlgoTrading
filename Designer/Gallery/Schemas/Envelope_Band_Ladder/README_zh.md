# Envelope Band Ladder 策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图在五分钟蜡烛上交易布林带均值回归，使用两级入场阶梯、受限反转、中轨退出和定时撤销挂单。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟蜡烛送入周期 20、宽度 1.5 的 Bollinger Bands。指标形成后，严格比较才检测收盘价低于下轨或高于上轨。
- 允许入场的 UTC 时间为 00:00:00 至 16:59:59，包含两端。空仓时，第一级以一单位市价单提交，第二级以一单位限价单挂在买入 `2 × lower − middle` 或卖出 `2 × upper − middle`。
- 当一至两级持仓遇到反向信号时，图先撤销旧限价单，再提交三单位市价单。这样可将任一允许的仓位转换为新方向的一至两单位，并且不再增加远端级别。
- 价格返回中轨的退出优先级低于同一根蜡烛上的反向带外入场。退出先撤销挂单，再串联两个一单位 ReduceOnly 市价动作；只有仍有第二单位时，第二个动作才会执行。
- 入场窗口之外，每日 Flag 同时触发批量撤单和定向撤单。时间条件不会强制平仓，中轨退出全天保持有效。

## 入场与出场规则

- **做多入场**: 在 UTC 入场窗口内，`Close < lower band` 且 `Position ≤ 0` 形成买入候选。空仓时提交一单位市价买单和位于 `2 × lower − middle` 的一单位远端限价单；持有空头时先撤销旧挂单，再买入三单位以反转受限仓位。
- **做空入场**: 在 UTC 入场窗口内，`Close > upper band` 且 `Position ≥ 0` 形成卖出候选。空仓时提交一单位市价卖单和位于 `2 × upper − middle` 的一单位远端限价单；持有多头时先撤销旧挂单，再卖出三单位以反转受限仓位。
- **离场**: 若不存在优先级更高的反向入场，多头在 `Close > middle band` 后退出，空头在 `Close < middle band` 后退出。先撤销待成交级别，再由两个串联的一单位 ReduceOnly 市价动作移除最多两个已成交级别且不会越过零仓位。时间过滤器只撤单，不强制平仓。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | 用于全部信号计算的已完成蜡烛周期。 |
| Bollinger Length | 20 | 仅在形成后输出的 Bollinger Bands 回看周期。 |
| Bollinger Width | 1.5 | 上轨与下轨采用的标准差倍数。 |
| Entry Start | 00:00:00 UTC | 固定 UTC 入场窗口的包含式开始时间。 |
| Entry End | 16:59:59 UTC | 固定 UTC 入场窗口的包含式结束时间；此后撤销挂单。 |
| Rung Volume | 1 | 每个普通阶梯级别及每个 ReduceOnly 退出步骤的数量。 |

## 图表详情

- [Candles](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 方块输出已完成的五分钟蜡烛，并可从随附的一分钟历史构建它们。[Indicator](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 方块计算三条布林线。
- [Converter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/converter.html)、[Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 和 [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) 方块提取 Close，并执行严格的带宽、仓位、时段及优先级门控。
- [Working time](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) 方块是图中的固定过滤器。Formula 与 Variable 方块在入场瞬间计算并保存远端价格以及三倍反转数量。
- 六个 [Order registering](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/register.html) 方块负责空仓市价入场、受限市价反转和两个挂单级别。[Mass order cancellation](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html) 方块标记每个撤单边界，两个定向撤单方块保存并撤销活动的远端限价单。
- 两个 [Modify position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 方块执行串联的 ReduceOnly 退出。[Chart panel](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) 显示蜡烛、三条布林线和策略成交数据流。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
