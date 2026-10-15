# 随机抛硬币基准策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表有意在没有市场信号的情况下进行交易。每当一根已完成的四小时K线到达且当前空仓时，一个随机值会在做多和做空入场之间作出选择。持仓会保持到随后十根已完成K线结束，再按市价平仓；下一根K线到来时可再次开始循环。这是用于与规则型系统比较的教学基准，并非实盘交易策略。

![schema](schema.svg)

## 策略概览

- 一条仅输出已完成K线的四小时K线流同时驱动随机决策和持仓周期计数器。
- Random 方块生成零到一之间的值。0.5 的阈值将该区间划分为两个互斥方向。
- 每根已完成K线到达时都会对当前持仓取样，并将样本值与零比较。两个入场方块还都使用 Open position 条件，因此只有在该K线开始时图表为空仓状态，才能开始交易。
- 入场成交会激活 N values 方块，该方块对随后十根已完成K线计数，然后才允许平仓。
- 该图表不使用指标、止损或止盈。随机序列未在图表内部设置种子，因此每次运行的结果可能不同。

## 入场与出场规则

- **做多入场**：随机值低于硬币阈值且当前空仓。图表按市价买入设定的交易量。
- **做空入场**：随机值大于或等于硬币阈值且当前空仓。图表按市价卖出设定的交易量。
- **离场**：入场成交后，图表对随后十根已完成的四小时K线计数。随后 N values 方块触发按市价平掉全部持仓。离场所在的K线不会开启另一笔交易；下一根已完成K线才是再次入场的第一个机会。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Hold Bars | 10 | 入场成交后、平仓前计数的已完成K线数量；该值必须大于零。 |
| Volume | 1 | 入场和离场订单量，单位为手。开仓和减仓使用相同的设定交易量。 |
| Coin Threshold | 0.5 | 低于该水平的随机值选择做多入场；大于或等于该水平的值选择做空入场。 |
| Candles | 04:00:00 | 用于入场决策和持仓周期计数的四小时K线周期；仅处理已完成K线。 |

## 图表详情

- [Candles](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 的输出送入 [Random](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/random.html) 方块、触发持仓快照、进入 [N values](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) 方块的 Input 端口，并送到 Chart panel。
- [Comparison](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 检查随机值是否至少达到硬币阈值。该信号选择做空分支，而处于 NOT 模式的 [Logical condition](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) 生成做多分支。
- [Position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html) 的输出送入一个由触发信号控制的 Variable 方块，用于在K线开始时保存持仓快照。该快照与共享的零常量进行比较，其空仓信号在两个入场 AND 方块中分别与方向信号汇合。这个快照可防止平仓成交后立即在同一根K线上重新开仓。
- 两个入场 [Modify position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 方块都使用市价单和 Open position 条件，并从同一个共享常量接收交易量。
- 做多和做空入场方块的 MyTrade 输出连接到 N values 方块的 Trigger 端口。在十根K线的计数期间，后续触发信号会被忽略。
- N values 的输出会触发两个处于 Reduce only 模式的 Modify position 方块。卖出方块只能减少多头持仓，买入方块只能减少空头持仓；两者都接收共享交易量，因此只有适用的分支会提交离场订单。
- [Chart panel](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/chart.html) 接收K线流以及两个入场和两个离场方块产生的成交。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后在相同的交易品种和时段上将其结果与规则型图表比较。请将此示例用作教学基准，而非实盘交易系统。
