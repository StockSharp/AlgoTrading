# 时钟K线突破策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本策略图每天按时钟选取一根参考K线，记住它的最高价和最低价，并在随后三根已完成的K线内交易对这两个价位的突破。EMA 20 过滤器决定突破的哪一侧可以交易，窗口到期时平掉持仓，而十根K线的冷却期在每一次被接受的入场之后让策略图暂时离场。

![schema](schema.svg)

## 策略概览

- 三十分钟K线在仍在形成时和已经完成后都会被推送。最终值 (Final value) 模块把这条数据流分开：参考价位、窗口计数器和冷却计数器只看已完成的K线，而突破判断读取当前正在形成的那根K线的收盘价。
- 工作时间 (Working time) 标出参考K线：开盘时间落在 02:30:00 与 02:59:59 之间的那根已完成K线。在三十分钟周期上每天恰好有一根K线符合条件，而半小时的区间也留出了余地——即使更改周期，每日一次的脉冲依然保留。
- 两对变量 (Variable) 模块从那根K线上取出价位。每一对中，第一个变量保存每根已完成K线的最高价（或最低价），只有在工作时间脉冲到来时才把它放行；第二个变量保存被放行的数值，并在每次K线更新时重复输出，因此在两根参考K线之间该价位始终留在连线上。
- 同一个脉冲启动一个设为三的 N 个值 (N values) 计数器。它统计已完成的K线，并在参考K线之后的第三根K线收盘时触发一次，交易窗口就此结束。
- 窗口状态是一个数值变量，通过组合 (Combination) 模块由三个来源写入：取得参考K线时写入 1，三根K线计数器触发时写入 0，入场被接受时立即写入 0。一个与零比较的比较 (Comparison) 模块把这个数字变成两条入场分支都读取的闸门，因此一个窗口最多产生一笔持仓。
- 做多需要收盘价高于参考最高价并且高于 EMA 20；做空需要收盘价低于参考最低价并且低于 EMA 20。每一侧都是一个逻辑条件 (Logical condition) AND，它同时还要求窗口处于打开状态、冷却期已过、持仓为空仓。
- 被接受的入场通过带有开仓 (Open position) 条件的修改持仓 (Modify position) 模块发出市价订单，因此同一根K线内重复出现的信号无法在第一笔订单之上再叠加一笔。同一个信号还启动第二个统计十根已完成K线的 N 个值计数器；第二对变量由各自的组合模块连接，把冷却标志保持为 0，直到计数结束后再把它恢复为 1。
- 三根K线计数器触发时，两个带有平仓 (Close position) 条件的修改持仓模块接收到这个信号。方向与未平持仓相反的那个按市价把持仓平掉；另一个没有可平的持仓，因而拒绝该信号。

## 入场与出场规则

- **做多入场**: 在参考K线之后的三根已完成K线内，若冷却期已过且持仓为空仓，收盘价高于参考最高价并且高于 EMA 20 时，通过开仓动作发出数量为 Order Volume 的市价买入订单。
- **做空入场**: 在同样这三根K线内，若冷却期已过且持仓为空仓，收盘价低于参考最低价并且低于 EMA 20 时，通过开仓动作发出数量为 Order Volume 的市价卖出订单。
- **离场**: 持仓按时间平掉，而不是按价格：三根K线的窗口计数器触发时，平仓模块把所有未平持仓按市价平掉。这里没有止损、没有止盈目标、也没有跟踪规则，因此持仓时间永远不会超过窗口；此外被接受的入场还会向窗口写入 0，所以同一个窗口不会被交易两次。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:30:00 | K线序列的时间周期。形成中的和已完成的K线都会被推送；只有已完成的K线定义参考价位并驱动两个计数器。 |
| EMA Length | 20 | 指数移动平均线的周期，它决定突破的哪一侧可以交易。只有在均线形成之后才会输出数值，因此在此之前无法入场。 |
| Reference From | 02:30:00 | 每日搜寻参考K线的时间区间的起点，按每根已完成K线的开盘时间读取。 |
| Reference Until | 02:59:59 | 该区间的终点。它与起点一起必须每天恰好覆盖一根K线的开盘时刻；默认的这一对数值正好覆盖一根三十分钟K线。 |
| Window Bars | 3 | 参考K线之后交易窗口持续的已完成K线数量。窗口到期时，同一个计数器平掉持仓。 |
| Cooldown Bars | 10 | 被接受的入场之后统计的已完成K线数量，此后策略图才被允许再次交易。 |
| Order Volume | 1 | 两个开仓动作使用的固定数量。平仓动作不需要数量，因为它们平掉所有未平的持仓。 |

## 图表详情

- [K线 (Candles)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 模块同样会发布形成中的和已完成的K线，而把它们区分开的正是[最终值 (Final value)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/final_value.html)。三个[转换器 (Converter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)分别在最终值之后读取 High 和 Low、在它之前读取 Close，正因如此，价位始终来自一根已完成的K线，而突破则是用实时价格来检验的。
- [工作时间 (Working time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) 读取送入其中的那根已完成K线的开盘时间，因此它的输出每天只对一根K线为真，而不是在一段实际时钟时间内为真。图上的连接顺序很重要：High 和 Low 转换器排在它之前，所以脉冲放行时，负责保存的[变量 (Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)里已经是当前这根K线的数值。
- 两个[N 个值 (N values)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)计数器各自由一个信号启动，并统计已完成的K线：交易窗口三根，冷却期十根。它们的输出在两个[组合 (Combination)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html)模块中与开启值和锁定值汇合，每个组合模块驱动一个状态变量，该变量在每次K线更新时重新输出自己的数字。
- [指标 (Indicator)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 模块提供 EMA 20。七个[比较 (Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)模块构成两个突破判断、两个趋势判断、窗口闸门、冷却闸门，以及针对每根K线保存的[持仓 (Position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html)快照所做的空仓检查；两个[逻辑条件 (Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND 模块把它们汇集成两条入场分支，而一个 OR 模块把其中任意一条分支变成启动冷却期的那个唯一信号。
- 四个[修改持仓 (Modify position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)模块负责动作：两个用开仓条件完成入场，两个用平仓条件完成按时间的离场。每一笔成交都由组合模块汇集，并与K线、EMA 20、两条参考价位以及全部四条订单流一起绘制在[图表面板 (Chart panel)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html)上。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
