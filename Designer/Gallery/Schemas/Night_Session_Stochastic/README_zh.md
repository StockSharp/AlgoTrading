# 夜盘时段随机指标策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

在四小时K线上运行的随机指标振荡器，只允许在夜间动作。有意思的地方是时钟而不是振荡器：夜盘时段从傍晚开始，到第二天早晨结束，因此它的开始时刻在一天之中比结束时刻更晚，而单个“工作时间”（Working time）模块无法描述这样的区间。于是本图把夜晚拆成两半——一半在午夜之前，一半在午夜之后——并在其他任何模块查看它之前，先把两半合成一个统一的答案。

![schema](schema.svg)

## 策略概览

- 整张图由已完成的四小时K线驱动，因此每个决策都建立在已收盘的K线上，任何环节都不会对仍在形成中的K线做出反应。
- 随机指标振荡器（Stochastic）在这些K线上运行，%K 取 14 根K线，%D 取 3 根，转换器（Converter）从它的取值中挑出 %K 线；%D 只用于绘图，从不参与交易。
- 两个工作时间（Working time）模块读取每根K线开盘的时刻：一个覆盖 21:00 到 23:59:59，另一个覆盖 00:00 到 06:00。它们中的任何一个单独都不构成夜晚。
- 一个设为“异或”（Exclusive or）的逻辑条件（Logical condition）模块把两半合成一个夜间信号。两半不可能重叠，因此同一时刻最多只有一半是打开的，而该模块在同时拿到两半结果之后，每根K线给出一次答案。
- 两个比较（Comparison）模块把 %K 与超卖、超买水平作对比；另外两个把持仓与零作对比，从而告诉本图当前是空仓、多头还是空头。
- 四个逻辑条件的“与”（And）门把这三项事实——夜间、振荡器、持仓——组合成两个入场和两个出场，因此在时钟不认可时，任何一个门都无法触发。
- 入场由运行在“开仓”（Open position）条件下的持仓修改（Position modify）模块完成：只有在持仓恰好为零时，才会发出 Order Volume 数量的市价委托，正是这一点让一个信号不会变成一连串的委托。
- 图表面板绘制K线、振荡器，以及本图产生的每一笔委托和成交，因此夜间窗口可以直接从图上读出来。

## 入场与出场规则

- **做多入场**: 当夜间窗口打开、持仓为零且 %K 低于超卖水平时，做多的门触发，持仓修改（Position modify）按市价买入 Order Volume。该模块上的“开仓”（Open position）条件意味着：在持仓再次平掉之前，同样读数的重复出现不会改变任何东西。
- **做空入场**: 镜像的一侧：夜间窗口打开、持仓为零且 %K 高于超买水平，做空的门触发，持仓修改在同样的“开仓”条件下按市价卖出 Order Volume。
- **离场**: 这里没有止盈、没有止损，也没有按时间强制平仓。多头由相反方向的极端读数平掉——持仓为多头时 %K 高于超买水平；空头则由持仓为空头时 %K 低于超卖水平平掉，两者都通过设为“平仓”（Close position）的持仓修改（Position modify）模块完成，该模块自行读取未平仓数量。出场同样位于夜间窗口之内，因此夜里开出的持仓会被持有过白天，到下一个夜里才被释放。平仓与反手是两个彼此独立的事件：平掉多头的那个极端读数只是让持仓归零，之后同类的又一次读数才会开出空头。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 04:00:00 | 四小时周期，由数据中可用的更小K线合成。只处理已完成的K线，每根K线以其开盘时刻计时，正是这一点决定它属于哪个时段。 |
| %K Length | 14 | %K 线的回看长度：振荡器用多少根K线来衡量收盘价。 |
| %D Length | 3 | %D 线的平滑长度。它只画在面板上，不参与任何条件。 |
| Evening Half From | 21:00:00 | 夜晚位于午夜之前那一半的开始时刻。请让两半彼此分开：它们应当在午夜相接，而不是互相重叠。 |
| Evening Half Until | 23:59:59 | 傍晚那一半的结束时刻。在午夜前一秒结束它，从而不会碰到紧随其后的那一半。 |
| Morning Half From | 00:00:00 | 夜晚位于午夜之后那一半的开始时刻，也就是午夜本身。 |
| Morning Half Until | 06:00:00 | 早晨那一半的结束时刻，同时也是夜晚的结束。过了这个时刻，图中任何一个门都无法触发，直到傍晚那一半重新打开。 |
| Oversold Level | 30 | 低于该水平时 %K 被视为超卖：持仓为零时它开出多头，持有空头时它平掉空头。 |
| Overbought Level | 70 | 高于该水平时 %K 被视为超买：持仓为零时它开出空头，持有多头时它平掉多头。 |
| Order Volume | 1 | 两个入场发出的数量。出场忽略它，直接平掉所有未平仓部分。 |

## 图表详情

- [K线（Candles）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 模块订阅四小时周期序列并打开只取已形成K线的开关，它给发出的每个值都打上该K线开盘时刻的时间戳。时钟模块读取的正是这个时间戳，因此一根K线属于其开盘小时所落入的那个时段，无论随后的四个小时里发生了什么。
- [指标（Indicator）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 承载随机指标振荡器，只有在指标形成之后才继续向下传递数值，因此历史数据最初的几根K线只用来启动计算，而不产生信号。[转换器（Converter）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) 从振荡器的取值中读出 %K 字段，把一个普通数字交给比较模块。
- 两个[工作时间（Working time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)模块正是本图的重点。每个模块只在一天中的时间处于自身那两个边界之间时为真，这意味着单个模块永远无法描述跨越午夜的窗口：它的开始时刻会晚于结束时刻，检查永远不可能通过。把夜晚在午夜处切开，就得到两个普通窗口，而它们上方的[逻辑条件（Logical condition）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)模块又把夜晚重新拼回一体。
- [持仓（Position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) 由三个[比较（Comparison）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)模块同时与一个取值为零的[变量（Variable）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)作对比——等于、大于、小于——正是这三个答案把两个入场门与两个出场门区分开来。超卖、超买水平以及委托数量同样是变量，因此本图所争论的每一个数字都是参数，而不是埋在某个模块里的取值。
- 四个[持仓修改（Position modify）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)模块按这些门动作。两个入场带有“开仓”条件，并从数量变量中取得手数；两个出场带有“平仓”条件且不需要数量，因为平仓委托的数量由它所平掉的持仓决定。它们的委托和成交与K线、振荡器一起送往[图表面板（Chart panel）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html)。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
