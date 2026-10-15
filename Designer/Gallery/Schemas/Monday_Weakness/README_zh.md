# 周一疲弱策略图示
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本图示按固定的周历进行交易。多空两个方向各有自己的日子：在做空日，当收盘价位于 SMA 20 下方时卖出；在回补日把该空头买回；在做多日，当收盘价位于 SMA 20 上方时买入；在离场日把该多头卖出。星期几直接从 K 线上以数字形式读出，因此四条日历规则就是四次普通比较；而由时钟驱动的入场时间窗口把每周的决策限制在一天中活跃的时段内。

![schema](schema.svg)

## 策略概览

- 五分钟 K 线只在收盘完成后推送。两个转换器 (Converter) 读取同一根 K 线：一个取收盘价，另一个取开盘时间对应的星期几，该值以数字形式给出，星期日为 0，星期六为 6。
- 四个变量 (Variable) 保存四个日历日——做空日、回补日、做多日和离场日；四个设为 Equal 的比较 (Comparison) 模块把星期数字变成四个信号。在任意一根 K 线上其中只可能有一个为真，正是这一点使四条分支永远不会相互争抢。
- SMA 20 在同一批 K 线上计算，在其形成之前不输出任何数值，因此运行开始后的前二十根 K 线完全不产生信号。两个比较模块读取它：一个在收盘价低于均线时为真，另一个在收盘价高于均线时为真。
- 一个持仓 (Position) 模块、一个保存零值的变量和一个设为 Equal 的比较模块共同构成空仓检查。两条入场分支都要求该条件，因此已经持有仓位的一周不会再叠加第二笔持仓。
- 当前时间 (Current time) 输入到工作时间 (Working time)，后者在 08:00:00 到 20:00:00 之间为真。两条入场分支同样要求处于该窗口内，因此每周的持仓永远不会在隔夜清淡的 K 线上开出。两个离场分支则被有意留在窗口之外——任何已开的持仓都必须在属于它的日历日内平掉，无论信号出现在几点。
- 两个逻辑条件 (Logical condition) AND 模块汇总入场条件。做空分支需要做空日、收盘价低于 SMA 20、持仓为空以及窗口开启；做多分支需要做多日、收盘价高于 SMA 20，以及同样的两道关卡。每条分支各驱动一个使用 Open position 条件的修改持仓 (Modify position) 模块，因此同一天内重复出现的信号不会再发出第二笔订单。
- 两个离场分支是使用 Close position 条件并指定明确买卖方向的修改持仓模块。回补日的模块是买入，因此只可能平掉空头；离场日的模块是卖出，因此只可能平掉多头。两者都不带成交量输入，因为 Close position 会依据已开的持仓本身确定订单数量。
- 图表面板 (Chart panel) 绘制 K 线序列、SMA 20、四个动作产生的全部订单及其成交，因此每周的节奏——周初入场、周中回补、周后段入场、周末离场——可以直接从图上读出。

## 入场与出场规则

- **做多入场**: 在做多日，处于入场窗口之内、持仓为空且收盘价高于 SMA 20 时，通过使用 Open position 条件的修改持仓模块发出数量为 Order volume 的市价买单。
- **做空入场**: 在做空日，处于入场窗口之内、持仓为空且收盘价低于 SMA 20 时，通过使用 Open position 条件的修改持仓模块发出数量为 Order volume 的市价卖单。
- **离场**: 离场依据的是日历，而不是价格。在回补日，一个使用 Close position 条件的修改持仓买单平掉已开的空头；在离场日，一个使用相同条件的修改持仓卖单平掉已开的多头。这里没有止损、没有止盈目标，也没有跟踪规则，因此持仓会一直持有到属于它的离场日到来。两个离场信号在其所属日期的每一根 K 线上都会重复出现，而且没有任何机制去计数或抑制这种重复：第一根 K 线平掉持仓，此后 Close position 已无仓可平，于是静默地拒绝该信号。入场也是如此——既没有按日计数器，也没有交易之间的冷却时间，正是 Open position 条件与空仓检查共同保证了一个日历日只发出一笔订单。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | K 线序列的时间周期。只推送收盘完成的 K 线；星期几、均线以及每个信号都从已完成的 K 线上读取。 |
| MA Period | 20 | 过滤两个方向入场的简单移动平均线周期。均线只有在形成之后才输出数值，因此在运行开始的最初几根 K 线上无法入场。 |
| Session From | 08:00:00 | 允许入场的每日时间窗口的起始时刻，取自策略时钟。离场不受该窗口约束。 |
| Session Until | 20:00:00 | 该窗口的结束时刻。放宽这一对参数，可让日历规则在任意时段生效；收窄它们，则把入场集中在一天中的几个小时内。 |
| Short day | 1 | 当收盘价低于均线时开出空头的星期数字。星期从星期日为 0 编号至星期六为 6。 |
| Cover day | 3 | 把已开空头买回的星期数字。它只平空头；在这一天，多头不会被触碰。 |
| Long day | 4 | 当收盘价高于均线时开出多头的星期数字。采用同样以星期日为 0 的编号方式。 |
| Exit day | 5 | 把已开多头卖出的星期数字。它只平多头；在这一天，空头不会被触碰。 |
| Order volume | 1 | 两个 Open position 动作使用的固定数量。两个 Close position 动作不需要成交量，因为它们会依据当前已开的持仓确定订单数量。 |

## 图表详情

- [K 线](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) (Candles) 模块设置为只接收收盘完成的 K 线，这一点有两重意义：星期几取自一根不会再变化的 K 线；图示发出的每一笔订单，都以已完成 K 线的收盘时间标记，而不是仍在形成中的 K 线的开盘时间。
- 日历和价格都来自作用于该序列的一对[转换器](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) (Converter)。从 K 线而不是从单独的时钟读取星期几，使日历判断与趋势判断保持完全相同的节拍，因此当[逻辑条件](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) (Logical condition) AND 模块汇总它们时，两者描述的始终是同一根 K 线。
- 四个星期数字只是普通的[变量](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) (Variable)，由[比较](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) (Comparison) 模块进行对比，因此整个周计划都可以通过参数列表重新安排：把做空日改成另一个数字，方案就改在那一天交易，而无需改动任何连线。
- [当前时间](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) (Current time) 与[工作时间](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) (Working time) 以一个简单的开关状态提供日内时段关卡：整个窗口内为真，窗口之外为假。它只连接到两条入场分支；旁边的[持仓](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) (Position) 快照以同样的方式提供空仓检查。
- 四个[修改持仓](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) (Modify position) 模块承担全部交易：两个使用 Open position 条件和固定成交量，两个使用 Close position 条件并指定买卖方向。给平仓模块指定方向，正是日历离场之所以精确的原因——回补日的买入根本不会去动多头，离场日的卖出也根本不会去动空头。它们发出的一切都会与 K 线和 SMA 20 一起绘制在[图表面板](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html) (Chart panel) 上。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
