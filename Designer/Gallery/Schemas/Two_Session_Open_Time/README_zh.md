# 双时段开盘时间策略图示
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本图示完全不使用任何指标：时钟是唯一的信号来源。交易日中的两个独立时间窗各开一次多头持仓，标志 (Flag) 让每个时间窗每天只入场一次，而第三个时间窗则平掉仍未了结的持仓，并为次日重新武装这两个入场窗口。

![schema](schema.svg)

## 策略概览

- 时间 (Time) 把当前时刻输出给三个工作时间 (Working time) 模块：两个入场窗口 09:30-14:00 和 00:00-04:00，以及一个强制平仓窗口 19:50-20:00。
- 每个入场窗口都通过设置为 And 的逻辑条件 (Logical condition) 与空仓检查相结合，因此只有在没有持仓时，窗口才能请求入场。
- 窗口会持续数小时保持开启，其闸门不断重复输出同一个 true 值。标志 (Flag) 位于闸门与委托之间，只放行其中的第一个，从而把一个长时间窗口变成一次入场。
- 两个窗口都做多。持仓修改 (Position modify) 以 Open position 条件运行，因此只有在持仓恰好为零时，才会发出 Order Volume 数量的市价委托。
- 强制平仓窗口驱动第三个设置为 Close position 的持仓修改 (Position modify)，而同一个信号还会重置两个标志，使两个入场窗口在次日重新就绪。
- 持仓保护 (Position protection) 监视两次入场的成交，并在 1.5% 止盈或跟随K线收盘价的 0.5% 跟踪止损处平掉持仓。
- 已完成的五分钟K线为整个图示定拍：它们把收盘价送往持仓保护，它们是面板所绘制的内容，它们的到达也推动时钟向前走。
- 图表面板显示K线、保护逻辑所依据的价格线、图示发出的每一笔委托以及收到的每一笔成交。

## 入场与出场规则

- **做多入场**: 在任一窗口内，只要持仓为空，该窗口的标志就会放出它的第一个 true 信号，持仓修改在 Open position 条件下按 Order Volume 以市价买入。同一窗口之后的每个信号都会被标志吞掉，直到平仓窗口将其重置。
- **做空入场**: 没有空头方向。两个窗口都开多头，图示发出的唯一卖出委托就是那些用来平掉已有多头持仓的委托。
- **离场**: 持仓保护在 1.5% 止盈或跟随K线收盘价的 0.5% 跟踪止损处平掉持仓。平仓窗口开始时仍未了结的任何持仓，都会被 Close position 动作平掉，该动作同时清除两个锁存标志。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | 五分钟周期；只处理已完成的K线，它们的收盘价既是保护逻辑检查的价格，也是构建图表线条的依据。 |
| First Window From | 09:30:00 | 第一个入场窗口的开始时间，按回放或服务器时间计。 |
| First Window Until | 14:00:00 | 第一个入场窗口的结束时间；此后该窗口不能再武装入场。 |
| Second Window From | 00:00:00 | 第二个入场窗口的开始时间，按回放或服务器时间计。 |
| Second Window Until | 04:00:00 | 第二个入场窗口的结束时间。 |
| Close Window From | 19:50:00 | 强制平仓窗口的开始时间，它会平掉未了结的持仓并重置两个锁存标志。 |
| Close Window Until | 20:00:00 | 强制平仓窗口的结束时间。 |
| Order Volume | 1 | 两个窗口入场共用的固定数量。 |
| Take Profit, % | 1.5 | 持仓保护平掉持仓所需的有利方向百分比变动。 |
| Stop Loss, % | 0.5 | 止损的不利方向百分比幅度；一旦价格朝有利方向运行，跟踪技术会把它沿K线收盘价上移。 |

## 图表详情

- [K线 (Candles)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 模块输出已完成的五分钟K线。[转换器 (Converter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) 取出它们的收盘价，这个价格既是 [持仓保护 (Position protection)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) 衡量止盈和跟踪止损的基准，也是画在K线旁边的那条线。除此之外没有任何基于价格的计算：图示中不含任何指标。
- [时间 (Time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) 向三个 [工作时间 (Working time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) 模块提供当前时刻。其中两个标出入场窗口，一个标出强制平仓窗口；随包提供的历史数据回放以 UTC 运行，因此窗口边界按 UTC 时间读取。
- [持仓 (Position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) 通过 [比较 (Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 与取值为零的 [变量 (Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) 相比较，得出空仓检查，两个 [逻辑条件 (Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) And 闸门共用这一检查，因此一笔未平持仓也会悄悄地把另一个窗口一并挡住。
- [标志 (Flag)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) 正是把窗口变成单次事件的部件。它的触发端是 And 闸门，复位端是平仓窗口；它只在被置位的那一刻传出数值，因此四小时窗口产生的数百次 true 读数被压缩成一次入场。
- 共有三个 [持仓修改 (Position modify)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 模块在工作：两个按 Order Volume 入场的 Open position，以及一个无需数量的 Close position 平仓——它会读取需要抵消的持仓。两次入场的成交由 [组合 (Combination)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html) 汇合后交给持仓保护，而保护自身的平仓成交会画在面板上，但不会回馈到它的成交输入端。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
