# Zigzag Candles 策略示意图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本示意图在连线上保持两个摆动价位 —— 最近五根已完成小时 K 线的最高价与最低价 —— 并在触及其中之一的那根 K 线上采取动作。一对互相复位的标志位（Flag）方块使每一次摆动只允许一次动作，因此同一波行情中被反复触及的价位只会产生一笔委托，不会更多。每次摆动先平掉已有持仓；其后的一次摆动才开出反方向的仓位。

![schema](schema.svg)

## 策略概览

- 整张图只由一条小时 K 线序列驱动，且只推送已完成的 K 线，因此任何价位、任何委托都不会建立在随后跳动仍可能推翻的价格之上。
- 两个转换器（Converter）分别读取每根 K 线的最高价与最低价，送入周期为 5 的最高值指标与周期为 5 的最低值指标。两个指标只有在形成之后才输出，因此在五根 K 线收盘之前不存在任何价位。
- 两个变量（Variable）方块保存这两个价位。每个方块接收最新的指标值但不因此放行输出，而是在每根 K 线上重新发布自己所保存的值，使该价位在每一根 K 线上都可供比较使用，而不仅仅在使其发生变化的那一根上可用。
- 另外两个转换器读取刚刚收盘那根 K 线的最高价与最低价，两个比较（Comparison）方块把它们与所保存的价位相比较：最高价触及或超过上方价位，最低价触及或跌破下方价位。
- 持仓（Position）快照每根 K 线锁存一次，并与零比较两次，得到“非空头”判断与“非多头”判断。两次突破分别与对应的判断经逻辑条件（Logical condition）AND 方块合并，因此只有在尚未持有空头时才对上方价位动作，只有在尚未持有多头时才对下方价位动作。
- 每个 AND 驱动一个标志位（Flag）的 Trigger，而相反方向的突破驱动该标志位的 Reset。标志位放行一次 true 之后即保持沉默，直到被复位为止，这就把同一波摆动中被多次触及的价位变成恰好一次动作。
- 被触发的标志位同时到达一个设为 Close position 的持仓修改（Position modify）方块和一个设为 Open position 的持仓修改方块。二者只有一个生效：已有持仓按市价平掉，空仓则按该次摆动所要求的方向开出，不适用的那个方块拒绝该信号。
- 图表面板绘制小时 K 线、两条极值线、全部三条委托流以及每一笔成交，因此摆动价位以及针对它们所采取的动作都可以从同一张图上读出。

## 入场与出场规则

- **做多入场**: 在尚未持有多头时，一根已完成 K 线的最低价触及或跌破所保存的下方价位。下方标志位触发一次：空头持仓按市价买回并变为空仓，空仓则转为数量为 Order Volume 的多头。此后无论最低价被再次触及多少次，该标志位都保持沉默，直到上方价位被触及为止。
- **做空入场**: 在尚未持有空头时，一根已完成 K 线的最高价触及或超过所保存的上方价位。上方标志位触发一次：多头持仓按市价卖出并变为空仓，空仓则转为数量为 Order Volume 的空头。该标志位保持沉默，直到下方价位被触及为止。
- **离场**: 没有止损、没有目标价，也没有计时器 —— 持仓由反方向的摆动来平掉。开出一侧仓位的那个标志位同时也接入 Close position 方块，因此另一侧价位第一次被触及时就会平掉一切已有持仓。由于开仓数量固定，而开仓方块只接受空仓状态，因此完整反手总是需要两次摆动：一次用于平仓，下一次才开出反方向的仓位。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 01:00:00 | K 线序列的时间周期。只推送已完成的 K 线，因此每根 K 线做出一次决策。 |
| Swing High Length | 5 | 上方极值所取的已完成 K 线根数。窗口越长，上方价位越难被触及、摆动越长；窗口越短，则几乎每根 K 线都会形成新的极值。 |
| Swing Low Length | 5 | 下方极值所取的已完成 K 线根数。它与上方参数分开设置，以便刻意把两侧做成不对称的。 |
| Initial Swing High | 999999999 | 在上方指标形成之前，上方价位所保存的值。它被刻意设为不可触及，从而在预热期间没有任何最高价能够触及它，也不会在真正的极值出现之前开出空头。 |
| Initial Swing Low | 0 | 在下方指标形成之前，下方价位所保存的值。成交价格无法自上方触及零，出于同样的原因这让多头一侧保持安静。 |
| Order Volume | 1 | 两个开仓动作所使用的数量。平仓动作不需要数量 —— 它平掉一切已有持仓 —— 因此在数量固定的情况下，持仓只会在一手空头、空仓和一手多头之间变动。 |

## 图表详情

- [K 线数据源（Candles）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)方块只订阅已完成的 K 线，正是这一点让价位是可信的。指标经由[转换器（Converter）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)供给，而转换器输出的是一个普通数值，该数值始终被视为最终值；因此一根仍在形成中的 K 线会像已完成那样进入最高值与最低值的窗口，极值最终会把自身包含进去。同一订阅方式也保证了委托的合法性：依据未完成 K 线的更新所生成的委托带有该根 K 线的开始时间，会因来自过去而被拒绝。
- 突破是相对于被判断的那根 K 线出现之前的价位来衡量的：保存价位的变量重新发布它们已经持有的值，而指标在同一根 K 线上才纳入这根新 K 线，因此一根 K 线所对照的价位是它之前那五根 K 线的极值，而不是把它自身也包含进去的极值。
- 两个[标志位（Flag）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html)方块交叉接线：上方突破复位下方标志位，下方突破复位上方标志位。这一对方块就是整张示意图的全部记忆 —— 它说明最近一次摆动的方向，以及是否已经据此动作过。标志位会忽略任一接口上的 false，因此比较方块在每次 K 线更新时发布的 false 不产生任何代价，只有在相反一侧的价位被触及之后它才会再次输出。
- 价位由[变量（Variable）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)方块保存，其输入端不作为触发；改由 K 线序列驱动它们的 Trigger。同样的方式把[持仓（Position）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html)值每根 K 线锁存一次，因此两个持仓[比较（Comparison）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)方块与两个[逻辑条件（Logical condition）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) AND 方块读到的是同一份快照，而不是在它们脚下不断变化的值。
- 三个[持仓修改（Position modify）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)方块执行动作，全部按市价：一个设为 Close position 并接入两个标志位，另外两个分别为每一侧设为 Open position。在一根外包 K 线上，即同时突破前一高点又跌破前一低点的 K 线，两个标志位可能在同一轮中一起触发并发出两笔市价委托；它们在持仓上相互抵消，但在日志和[图表面板（Chart panel）](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html)上都会显示为成交。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
