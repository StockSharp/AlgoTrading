# EMA 交叉成交提醒策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表在已完成的一分钟 K 线上交易 120 周期快速 EMA 与 450 周期慢速 EMA 的向上和向下交叉。仓位快照过滤每个信号，固定数量的市价单管理敞口，策略自身的每笔成交都会写入日志，图表则显示 K 线、两条 EMA 以及买入和卖出成交。

![schema](schema.svg)

## 策略概览

- 已完成的一分钟 K 线同时送入快速 EMA 120 和慢速 EMA 450。两个指标均关闭仅输出已形成值的过滤，因此从计算开始即可提供数值。
- Crossing 方块在快速 EMA 向上穿越慢速 EMA 时输出 `true`。NOT 方块把向下交叉事件的 `false` 转换为卖出侧的正触发信号。
- 每根 K 线评估时，由 K 线触发的快照会在处理 EMA 信号之前输出当前仓位。比较条件仅在 `Position <= 0` 时允许买入，仅在 `Position >= 0` 时允许卖出。
- 两个市价单方块都使用 `NoCondition` 和固定 Volume 1。反向信号可以缩减仓位、使仓位归零，并可在仓位绝对值小于 Volume 时穿过零点，但不保证完成反转。
- Strategy trades 方块将策略自身的每笔成交按固定的成交消息模板发送到 Log 通知。图表接收已完成的 K 线、两条 EMA 以及买入和卖出成交数据流。

## 入场与出场规则

- **做多入场**: 当快速 EMA 向上穿越慢速 EMA，且该 K 线时刻的仓位快照小于或等于零时，该策略图提交数量为 1 的市价买单。
- **做空入场**: 当快速 EMA 向下穿越慢速 EMA，且该 K 线时刻的仓位快照大于或等于零时，该策略图提交数量为 1 的市价卖单。
- **离场**: 图表不包含专用离场或保护方块。之后满足条件的反向固定数量订单可以缩减当前仓位、平掉等量仓位，或在仓位绝对值小于 Volume 时穿过零点；不保证完成反转。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:01:00 | 一分钟 K 线周期；只有已完成的 K 线才会驱动 EMA 和决策链。 |
| Fast EMA Period | 120 | 快速 ExponentialMovingAverage 的周期；仅输出已形成值的过滤已关闭。 |
| Slow EMA Period | 450 | 慢速 ExponentialMovingAverage 的周期；仅输出已形成值的过滤已关闭。 |
| Volume | 1 | 提供给两个 NoCondition 市价单方块的固定数量。 |

## 图表详情

- [K 线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)方块仅输出已完成的一分钟 K 线，并在将数据送入 EMA 计算之前先触发仓位快照。
- 两个[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)方块分别计算周期为 120 和 450 的 ExponentialMovingAverage。它们的仅输出已形成值选项均为 `false`，两个输出也同时发送到图表。
- [交叉](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)输出在向上交叉时为 `true`，向下交叉时为 `false`。NOT [逻辑条件](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)把向下事件转换为正的卖出触发信号；独立的 AND 方块组合方向和仓位。
- 当前[仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html)会持续保存，并在 EMA 路径前每根 K 线输出一次。[比较](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)方块在与交叉相同的 K 线因果处理链中计算 `Position <= 0` 和 `Position >= 0`。
- 买入和卖出[修改仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)方块使用 `NoCondition` 和共享的固定 Volume 提交市价单。图表不设止损、止盈或独立离场方块。
- [策略成交](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html)输出本策略的每个 `MyTrade`。[字符串格式化](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html)严格使用 `EMA cross fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}`。
- [通知](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)方块以 Type `Log` 和 Caption `EMA cross trade` 写入每笔已格式化成交。图表绘制 K 线、快速 EMA、慢速 EMA 以及独立的买入和卖出成交数据流。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
