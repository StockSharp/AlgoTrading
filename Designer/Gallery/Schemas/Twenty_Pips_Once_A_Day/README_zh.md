# 每日一次二十点策略示意图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本示意图每天最多建立一笔逆势持仓。每天一次，在选定的整点时刻、且仅在账户空仓时，它把已收盘的 1 小时 K 线收盘价与 29 根之前那根 K 线的收盘价作比较，并对该窗口的漂移方向反向下注：下跌之后买入，上涨之后卖出。离场则交给较小的止盈、较宽的止损以及对持仓存续时间的硬性上限。

![schema](schema.svg)

## 策略概览

- 一切都由已收盘的 1 小时 K 线驱动。在仍在形成的 K 线内部不做任何计算，因此每个决策都基于已收盘的价格作出。
- 前值（Previous value）保存 29 根之前的那根 K 线。它的收盘价与当前收盘价比较，衡量的是大约最近 1.25 天的价格漂移。
- 比较结果决定与该漂移相反的方向：较早的收盘价高于当前收盘价，说明市场下跌，方案买入；较早的收盘价低于当前收盘价，说明市场上涨，方案卖出。这里用的是两个严格比较，因此窗口结束价与起点完全相同时，不会产生任何信号。
- 时间（Time）提供与刚刚收盘的那根 K 线相对应的时钟读数，转换器（Converter）取出其中的小时数，再与 Trading Hour 参数比较，从而每天为一根 K 线打开入场窗口。
- 当前持仓必须为零。它与每天一次的小时过滤以及入场模块上的「开仓（Open position）」条件共同作用，使方案同一时间只持有一笔持仓。
- 两个方向的入场都是固定数量的市价委托。它们的成交被合并后交给持仓保护（Position protection），由其在 0.1% 止盈或 0.5% 止损处平仓，正是该思路所依据的一比五比例。
- 被接受的入场会启动 N 个值（N values）计数器，它统计 21 根已收盘 K 线。计数用尽时，设置为「平仓（Close position）」的持仓修改（Position modify）模块会平掉仍然敞开的部分，于是既未触及止盈也未触及止损的持仓不会被无限期持有。
- 是否允许交易（Is trade allowed）监视平台的实盘交易许可。每次入场被接受时，方案记录当时的许可状态并向日志写入一行，这是报告而非否决：在历史回放中该许可永远不会被授予，若以它来限制入场，整个示意图就会完全沉默。

## 入场与出场规则

- **做多入场**: 当一根已收盘的 1 小时 K 线其时钟小时等于 Trading Hour、持仓为零、且 29 根之前的收盘价高于当前收盘价时，在「开仓（Open position）」条件下以市价买入 Volume。
- **做空入场**: 当一根已收盘的 1 小时 K 线其时钟小时等于 Trading Hour、持仓为零、且 29 根之前的收盘价低于当前收盘价时，在「开仓（Open position）」条件下以市价卖出 Volume。
- **离场**: 持仓保护（Position protection）按自入场成交价起算的 0.1% 止盈或 0.5% 止损平仓，其价格检查由 K 线收盘价驱动。若两个水平都未触及，N 个值计数器会在入场之后第 21 根已收盘 K 线上触发，由「平仓」模块平掉剩余部分；如果保护已经平掉了持仓，该动作找不到可平的数量，也就什么都不做。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 01:00:00 | 工作 K 线的时间周期。只处理已收盘的 K 线，因此委托的时间绝不会落在仍在形成中的 K 线内部。 |
| Lookback Bars | 29 | 参考收盘价取自多少根之前的 K 线。这就是入场所要反向操作的那段漂移窗口的宽度。 |
| Trading Hour | 7 | 每日入场窗口开启的时钟小时，取自与已收盘 K 线一同到达的策略时间。 |
| Volume | 0.1 | 两个入场委托的固定数量。没有自适应的仓位调整：每次入场的数量都相同。 |
| Max Position Bars | 21 | 一笔持仓在被无条件平掉之前可以存续多少根已收盘 K 线，无论盈亏。 |
| Take Profit % | 0.1 | 持仓保护平仓所需的有利变动幅度，以入场价格的百分比表示。 |
| Stop Loss % | 0.5 | 持仓保护平仓所需的不利变动幅度，以入场价格的百分比表示。该止损是固定的，不是移动止损。 |

## 图表详情

- [K线（Candles）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 模块设置为只输出已收盘的 K 线，[前值（Previous value）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) 作用于 K 线本身而不是某个价格，其后接一个[转换器（Converter）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)。两个[比较（Comparison）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)模块把两个收盘价转换成多头方向和空头方向；由于两者都是严格比较，价格没有变化的窗口两个方向都不会产生。
- [时间（Time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) 在这里是数据源而不是标签：转换器读取它的 Hour，比较模块把它与一个[变量（Variable）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)相匹配。小时判断和空仓判断都锚定在 K 线上，因为它们所比较的常量由 K 线数据流触发，所以入场闸门每根已收盘 K 线只能通过一次。
- [持仓（Position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) 与对零的比较提供空仓检查，两个[逻辑条件（Logical condition）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)模块把漂移、小时和持仓汇总成每个方向一个信号。两个[持仓修改（Position modify）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)模块都带有「开仓（Open position）」条件，这是防止持仓未平时重复入场的第二道保护。
- 被接受的信号还会启动 [N 个值（N values）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)，它统计已收盘 K 线，然后触发第三个设置为「平仓（Close position）」的持仓修改模块。该模块不接收数量：要平掉的数量由未平仓持仓推导得出，账户空仓时就不会产生任何委托。
- [组合（Combination）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) 把两个入场方向的成交合并后送给[持仓保护（Position protection）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)。与此并行，[标志（Flag）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html)每次入场恰好放出一个脉冲，并由存续时间计数器复位；该脉冲把[是否允许交易（Is trade allowed）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html)的读数锁存到一个变量中，再由[字符串格式化（String format）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html)模块转换成每建立一笔持仓一行的[通知（Notification）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)日志。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
