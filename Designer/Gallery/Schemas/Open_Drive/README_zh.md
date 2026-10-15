# Open Drive 策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本图示只抓取一根脉冲 K 线：其实体大于当前平均真实波幅的某一比例。实体的颜色决定方向，SMA 20 必须与之一致，时间必须落在 UTC 当日的前六个小时之内，并且必须处于空仓状态。止盈和止损是唯一的离场方式。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟 K 线通过两个转换器 (Converter) 提供收盘价和开盘价，并驱动 SMA 20 与 ATR 14。两个指标都只在形成后才输出，因此在各自积累足够的 K 线之前，任何比较都不会给出结论。
- 一个公式 (Formula) 用 `abs(Close - Open)` 计算当前 K 线的实体；另一个把当前 ATR 换算成阈值 `ATR x 0.3`。当实体严格大于该阈值时，比较 (Comparison) 判定这根 K 线为脉冲，因此图示据以行动的这根 K 线相对于当时的波动率异常之大。
- 两个比较读取同一根 K 线的颜色，即 `Close > Open` 与 `Close < Open`；另外两个读取它相对 SMA 20 的位置，即 `Close > SMA` 与 `Close < SMA`。仅有脉冲绝不会交易：颜色与趋势必须指向同一方向。
- 当前时间 (Current time) 模块把策略时间输送给覆盖 UTC 00:00:00 至 06:00:00 的交易时间 (Working time) 模块。它的真/假结果保存在变量 (Variable) 中，并在 K 线到达时重新发布，因此时段过滤与所有价格比较在同一时刻得出结论，而不是按自己的时钟运行。
- 另一个变量在每根 K 线上对持仓做快照，与零比较即可判断图示是否空仓。通过快照读取持仓，可以避免落在两根 K 线之间的成交在 K 线中途重新触发入场逻辑。
- 多头的逻辑条件 (Logical condition) 为 `impulse AND bullish body AND close above SMA AND inside the window AND flat`；空头则完全镜像。每个都等待全部五个输入，因此每根已完成的 K 线恰好输出一个结论。
- 结论为真会触发运行在 OpenPosition 模式下的修改持仓 (Modify position) 模块，它按设定的数量发送市价订单，并且只有在持仓确实为零时才会执行。因此一根 K 线永远不会开出两笔交易，而已有持仓则完全阻止新的入场。
- 两个方向的入场成交都经由组合 (Combination) 进入持仓保护 (Position protection)，后者依据此后每根已完成 K 线的收盘价来执行 3% 止盈和 2% 止损。

## 入场与出场规则

- **做多入场**: 在 UTC 00:00:00-06:00:00 之内，SMA 20 与 ATR 14 已形成且持仓为空时：`abs(Close - Open) > ATR x 0.3`、`Close > Open` 且 `Close > SMA 20` 会发出一手的 OpenPosition 市价买入。
- **做空入场**: 在 UTC 00:00:00-06:00:00 之内，SMA 20 与 ATR 14 已形成且持仓为空时：`abs(Close - Open) > ATR x 0.3`、`Close < Open` 且 `Close < SMA 20` 会发出一手的 OpenPosition 市价卖出。
- **离场**: 没有基于信号的离场，也没有反手。持仓保护在相对入场成交价盈利 3% 或亏损 2% 时平仓，判定依据是每根已完成 K 线的收盘价，因此 K 线内部穿越价位的瞬时波动要等到该 K 线走完才会被处理。图示在两笔交易之间不设冷却计数：持仓一旦平掉，窗口内下一根满足条件的 K 线就可以再次开仓。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | 已完成 K 线的周期；所有比较、两个指标以及保护价位都按其收盘价计算。 |
| MA Period | 20 | 简单移动平均线的周期，该指标只在形成后输出，用于判断 K 线收在趋势的哪一侧。 |
| ATR Period | 14 | 平均真实波幅的周期，该指标只在形成后输出，用于描述当时正常的 K 线幅度。 |
| ATR Multiplier | 0.3 | K 线实体必须超过的当前 ATR 比例，超过才算脉冲。调高会要求更罕见、更大的 K 线；调低则会接受普通的 K 线。 |
| Window Begin | 00:00:00 | 交易窗口的起始时间（UTC）。在此之前，脉冲仍会被计算和绘制，但不会交易。 |
| Window End | 06:00:00 | 交易窗口的结束时间（UTC）。把这一对参数放宽到 00:00:00-23:59:59，图示即可全天候交易。 |
| Order Volume | 1 | 两个入场发送的数量；持仓始终是一手，因为在持仓存在期间第二次入场会被拒绝。 |
| Take Profit | 3% | 止盈距离，按入场成交价的百分比计算。 |
| Stop Loss | 2% | 止损距离，按入场成交价的百分比计算。 |

## 图表详情

- [K线 (Candles)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) 模块输出已完成的五分钟 K 线，随包的分钟历史数据即可构建。两个[转换器 (Converter)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)读取收盘价和开盘价，两个只在形成后输出的[指标 (Indicator)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)模块计算 SMA 20 与 ATR 14。
- 两个[公式 (Formula)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html)模块构建实体和 ATR 阈值，五个[比较 (Comparison)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)模块把实体、颜色、趋势方向和持仓转换为信号。
- [当前时间 (Current time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/current_time.html)模块把策略时间送入[交易时间 (Working time)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/working_time.html)，后者的结果变化远比 K 线频繁。关闭了 Input as trigger 的[变量 (Variable)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)保存该结果，只有当下一根 K 线触发时才释放。
- [持仓 (Position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html)由第二个变量做快照，并与常数零比较。两个五输入的[逻辑条件 (Logical condition)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)模块都会等待全部输入，因此每根 K 线产生一个多头结论和一个空头结论。
- 两个 OpenPosition 模式的[修改持仓 (Modify position)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)模块以市价交易。[组合 (Combination)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/combination.html)把两个方向的入场成交合并后送入[持仓保护 (Position protection)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)，[图表面板 (Chart panel)](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/chart.html)绘制 K 线、SMA、ATR、包括保护性订单在内的所有订单以及每一笔成交。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
