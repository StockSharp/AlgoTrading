# 带资金守护的定时双腿篮子组合
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

这张图交易的是时钟，而不是信号。它完全不使用任何指标：每天开启一个短暂的入场窗口，图在同一时刻买入两个品种；稍后的另一个窗口把两条腿一起平掉；而在两个窗口之间，资金守护会盯住这个篮子组合已经赚到的钱，并在触及盈利目标或亏损上限时提前把它平掉。

![schema](schema.svg)

## 策略概览

- 两个类型为 Security 的变量（Variable）块指定了图所交易的两个品种。每个变量各自驱动一条自己的K线序列、一个自己的开仓动作和一个自己的平仓动作，因此两条腿在一起开、一起平的同时，手数和管理仍然是分开的。
- 同步（Sync）把两条 5 分钟K线序列保持住，直到两条腿都送出同一根K线，再把它们作为一组一起放行，因此为篮子组合绘制的数值绝不会把一条腿的最新价格和另一条腿的陈旧价格混在一起。
- 公式（Formula）把放行后的每个收盘价乘以该腿的交易手数，再把两个乘积相加。结果就是按图实际交易的手数计算出的篮子组合价值，它也正是画在图表面板上的那条线。
- 工作时间（Working time）读取第一条腿已完成K线的时间戳，只在入场窗口之内开启，因此每天恰好只有一根K线能够通过它。标志（Flag）把这次开启变成一个单次脉冲，并一直保持锁存，直到篮子组合被平掉为止。
- 锁存后的脉冲触发两个设置为 Open position 的持仓修改（Position modify）块。每个块都带有自己的 Security 和自己的 Volume，而 Open position 条件会在该品种已经有持仓时让这一对块保持沉默，因此一个脉冲绝不可能在已有的篮子组合之上再叠加第二个。
- 策略盈亏（Strategy P&L）驱动一个公式，把已实现盈亏与浮动盈亏相加。第二个公式再减去篮子组合开仓那一刻记录下来的金额，从而把账户的累计总额变成当前这一个篮子组合单独的结果。
- 一个逻辑 OR 汇集了三个平仓理由：平仓窗口、篮子组合结果高于盈利目标、以及篮子组合结果低于亏损上限。它的信号驱动两个设置为 Close position 的持仓修改块，同时释放每日标志，好让下一个入场窗口发现锁存已经空出来。

## 入场与出场规则

- **做多入场**: 在入场窗口之内，第一条腿的已完成K线打开工作时间，当天的标志还没有被用掉，两个 Open position 块在同一个脉冲上同时触发：一个按自己的手数买入第一条腿，另一个按自己的手数买入第二条腿。每一笔订单都是市价订单；如果某个品种上已经持有仓位，则针对该品种的订单会被抑制。
- **做空入场**: 这张图没有做空的一侧：两个入场块都固定为 Buy 方向。做空的篮子组合只差一个设置——把两个 Open position 块的 Direction 改成 Sell，同样的时间表、同样的锁存和同样的资金守护就会把这个篮子组合反向运行。
- **离场**: 有三个理由可以平掉篮子组合，其中任何一个成立就足够。平仓窗口由策略时钟而不是由K线的到达驱动，因此按时平仓；篮子组合结果升到盈利目标之上，就提前在盈利中平掉；篮子组合结果跌到亏损上限之下，就提前在亏损中平掉。三者都经由同一个逻辑 OR 进入两个 Close position 块，这两个块既不需要手数也不需要方向，因为二者都由它们找到的持仓算出——而在没有持仓时它们什么都不做，所以窗口内重复出现的信号是无害的。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| First Leg Security | BTCUSDT@BNBFT | 第一条腿所交易的品种；它驱动着为入场窗口计时的那条K线序列。 |
| Second Leg Security | TONUSDT@BNBFT | 第二条腿所交易的品种；它与第一条腿在相同的信号上开仓和平仓。 |
| First Leg Candles | 00:05:00 | 第一条腿的时间周期。只使用已完成的K线，因此入场窗口至少要有一根K线那么宽。 |
| Second Leg Candles | 00:05:00 | 第二条腿的时间周期。请与第一条腿保持一致，因为在计算篮子组合价值之前，两者要被一起保持住。 |
| Alignment Interval | 00:05:00 | 用于对齐两条腿的分组间隔。它应当与K线的时间周期一致；取更大的值会让这一对数据晚于它们所属的那根K线才被放行。 |
| Entry Window From | 10:00:00 | 每日入场窗口的起点，从第一条腿已完成K线的时间戳读取。 |
| Entry Window Until | 10:04:00 | 每日入场窗口的终点。两个值之间的跨度必须恰好包含一根K线的开启，否则每日锁存就会被不止一次地置位。 |
| Flatten Window From | 17:00:00 | 每日平仓窗口的起点，从策略时钟读取，而不是从K线的到达读取。 |
| Flatten Window Until | 17:10:00 | 每日平仓窗口的终点。请让它有几根K线那么宽，以便时钟至少在其中被采样一次。 |
| First Leg Size | 0.01 | 用于开出第一条腿的数量，也是第一条腿在所绘篮子组合价值中占有的权重。 |
| Second Leg Size | 100 | 用于开出第二条腿的数量，也是第二条腿在所绘篮子组合价值中占有的权重。选择它时应使两条腿贡献的金额大致相当。 |
| Profit Target | 100 | 当前篮子组合赚到的钱超过该值，就提前把它平掉。它从篮子组合开仓的那一刻起计量，而不是从运行开始起计量。 |
| Loss Limit | -500 | 当前篮子组合亏掉的钱低于该值，就提前把它平掉；这是一个负值，计量方式与盈利目标相同。 |

## 图表详情

- 两个类型为 Security 的[变量（Variable）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)块是整张图中唯一指定品种的地方。每个变量驱动一个[K线（Candles）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)块，以及管理该腿的那两个持仓修改块的 Security 输入，因此要把一条腿改指向另一个品种，只需要修改一个值。
- [同步（Sync）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/sync.html)每条腿各接入一条线，等到两边的K线都走完之后再一起放出。放行的K线画在图表面板上，并被转换成收盘价，由[公式（Formula）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html)按交易手数加权，形成篮子组合价值曲线。
- 两个时钟是有意做得不一样的。入场用的[工作时间（Working time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)由第一条腿的K线驱动，因此入场决策不会偏离它所依据的价格；平仓用的工作时间由[时间（Time）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html)驱动，因此即使行情数据安静下来，平仓照样会发生。[标志（Flag）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html)位于入场窗口和订单之间，只有平仓信号才会把它复位，这正是把整张图限制为每天一个篮子组合的原因。
- [盈亏变化（P&L change）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html)在每次更新时报告已实现盈亏和浮动盈亏。把两者相加，得到账户的累计总额；一个变量在第一笔入场成交时记录下这个总额，第二个变量保存它并在之后的每次更新时重新发出，相减之后剩下的就是当前持有的这个篮子组合的结果。因此守护衡量的是当前的篮子组合，而不是账户的整个存续期间，并且篮子组合一被平掉，它就回到零。
- 每一个动作都是一个[持仓修改（Position modify）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)块。两个入场块使用 Open position 条件，并带有明确的方向和手数；两个出场块使用 Close position，它既不接收方向也不接收手数，而是由自己品种的当前持仓推导出这两者。[组合（Combination）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html)把四条成交流合并成画在图表面板上的单一交易序列，而四条订单流则分别绘制。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
