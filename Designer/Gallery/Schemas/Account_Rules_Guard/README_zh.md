# 账户规则守护策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本图在完成的一分钟K线上交易 EMA(120)/EMA(450) 的交叉，并把这一普通的入场包裹在一个账户级监管器中。盈亏变化 (P&L change)、一个公式、两个比较、一个逻辑 OR、一个标志 (Flag) 锁存以及一个存储起来的标志，共同监视本次运行的合并资金结果。一旦该结果达到亏损限额或盈利目标，监管器就平掉持仓、把发生的事写入日志，并在本次运行结束前阻止一切后续入场。

![schema](schema.svg)

## 策略概览

- 完成的一分钟K线送入两个指标 (Indicator) 模块——一条快速 EMA 和一条慢速 EMA，两个交叉 (Crossing) 模块从两个方向读取这一对均线。
- 盈亏变化 (P&L change) 在同一次更新中报告已实现和未实现资金，公式 (Formula) 把两者相加为一个合并结果，该结果在每次盈亏更新时重新计算。
- 两个比较 (Comparison) 模块把合并结果与 Max Loss 水平和 Profit Target 水平相比较，使用 OR 运算符的逻辑条件 (Logical condition) 把其中任一答案变成单一的规则触发信号。
- 标志 (Flag) 在该信号第一次为真时把它锁存。它的复位输入被刻意留作未连接，因此在本次运行余下的时间里，监管器是一个单向开关。
- 一个标志类型的变量 (Variable) 保存锁存状态，并在每根K线上重新发出它，这正是逻辑 AND 工作所需要的；使用 NOT 运算符的逻辑条件把这个存储状态变成入场闸门所读取的许可。
- 每个入场闸门都是三项内容的逻辑 AND：本方向上的交叉、监管器许可，以及由当前持仓 (Current position) 与对零的比较构成的持仓检查。
- 持仓修改 (Position modify) 在开仓条件下按市价开出一个 Volume，因此只有在空仓时才会入场；反向交叉送入第二个持仓修改模块，它平掉已开的持仓，让图形回到空仓而不是反手。
- 当规则触发时，第三个持仓修改模块把持仓平掉，一个变量在那一刻对结果拍下快照，字符串格式化 (String formatter) 将其渲染出来，通知 (Notification) 把它连同描述平台交易许可的第二行一起写入日志。

## 入场与出场规则

- **做多入场**: 在一根完成的K线上，快速 EMA 上穿慢速 EMA，且监管器未触发、持仓不为多头时，按市价买入一个 Volume。开仓条件意味着只有在空仓时才会入场：同样的信号若在已有持仓时到达，会被拒绝，而不是加到持仓上。
- **做空入场**: 在一根完成的K线上，快速 EMA 下穿慢速 EMA，且监管器未触发、持仓不为空头时，按市价卖出一个 Volume。与多头一侧一样，开仓条件只允许从空仓入场。
- **离场**: 普通的离场是反向交叉：平仓的持仓修改模块把一切已开的持仓平掉，因此图形回到空仓并等待新的交叉，而不是把持仓反手。紧急离场则是监管器：一旦已实现与未实现的合并结果达到 Max Loss 水平或 Profit Target 水平，就按市价平掉持仓、置位锁存、把金额和平台许可写入日志，并且在本次运行余下的时间里不再接受任何入场。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:01:00 | K线序列的时间框架。只有完成的K线才驱动移动平均线、重新发出的锁存状态、成交量脉冲和许可读取。 |
| Fast EMA Length | 120 | 快速指数移动平均线的周期。 |
| Slow EMA Length | 450 | 慢速指数移动平均线的周期。 |
| Max Loss | -5000 | 以账户货币计的已实现与未实现合并结果，达到或低于该值时监管器触发。它写成负数，并且刻意设得较宽：限额设得太近，会在图形交易到足以显示任何东西之前就把它停下。 |
| Profit Target | 10000 | 达到或高于该值时监管器触发的已实现与未实现合并结果。达到它会以与亏损相同的方式结束本次运行：平掉持仓、置位锁存、不再入场。 |
| Volume | 1 | 两个入场模块使用的固定数量。两个平仓模块的数量取自已开的持仓，并忽略该值。 |

## 图表详情

- [盈亏变化 (P&L change)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) 把已实现和未实现资金一起发出，[公式 (Formula)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) `r + u` 把它们相加，得到两个[比较 (Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 模块所判断的那个值。在账户实际发生变动之前，该模块保持沉默，因此监管器不会在第一笔成交之前触发；两个限额变量由合并结果本身触发，从而保证每个比较的两侧总是在同一次更新中到达。
- 许可是被存储的，而不是被持续发出的。[标志 (Flag)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) 只在它被置位的那一刻发出，而逻辑 AND 无法使用这一点，因为 AND 会等待每个输入上都出现值，并在触发后把它们清空。因此锁存状态存放在一个标志类型的[变量 (Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) 中，它的默认值为 false，它的 Trigger 输入是K线流：每根K线它都重新发出当前状态，而使用 NOT 运算符的[逻辑条件 (Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) 把它变成入场许可。
- 在触发信号上使用 OR 运算符是刻意的选择：与 AND 不同，它不会等待每个输入上都出现值，因此任一限额单独就能把它抬起。它还会在每次平静的更新上发出一个 false 答案，而这在下游不会有任何代价：标志忽略 false 触发，快照变量忽略它，[持仓修改 (Position modify)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 拒绝据此行动，因此否定的答案永远不会发出订单。
- 平仓的模块使用平仓条件，完全不需要成交量输入：数量取自已开的持仓。入场模块保留各自的 Volume，而[当前持仓 (Current position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) 与零的比较，为每个闸门提供了入场规则所述的同样的“非多头”和“非空头”检查。
- [是否允许交易 (Is trade allowed)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) 在每根K线上读取平台自身的许可，而它被刻意排除在入场闸门之外：在录制的历史数据上，它在整个运行期间都回答“不允许”，因此建立在它之上的闸门永远不会打开，图形也就根本不会交易。它的答案由一个变量捕获，并由[字符串格式化 (String formatter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) 渲染进第二行[通知 (Notification)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)，那才是它该在的位置：它解释规则触发那一刻平台的状态，而不是让图形沉默。通知被设为日志类型，这是在回放历史数据时唯一会被投递的类型。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
