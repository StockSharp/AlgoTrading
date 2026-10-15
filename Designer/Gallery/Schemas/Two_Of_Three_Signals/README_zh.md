# 三选二方向信号策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表在每根已完成的30分钟K线上评估三个方向投票：MACD Signal线的斜率、Stochastic %K区间和RSI区间。任意一对一致的投票都会为该K线生成一次多数事件。冷却就绪时，空仓以数量1开仓；若持有反向仓位，则先用ReduceOnly平仓，再于该平仓单确认完全成交后沿新方向入场。每次新仓成交都会启动十根K线的冷却期。

![schema](schema.svg)

## 策略概览

- 已完成的30分钟K线输入只输出已形成值的MACD 12/26/9、Stochastic 14/3和RSI 14指标。Previous value方块保留前一根K线的MACD Signal值，历史就绪门控在该值存在之前禁止决策。
- 做多投票为`MACD Signal > Previous MACD Signal`、`Stochastic %K ≤ 20`和`RSI < 40`。做空投票为`MACD Signal < Previous MACD Signal`、`Stochastic %K ≥ 80`和`RSI > 60`。相等的MACD值以及方向区间之外的振荡指标值均为中性。
- 每个方向使用三个两两组合的Logical condition方块，表示所有可能的两票多数。每根K线的Flag只放行第一个满足条件的组合，因此即使三个指标全部一致，也只生成一次方向事件，而不是三次。
- 持仓和冷却状态快照为每次多数事件选择路径。冷却就绪且空仓时，提交一笔Order Volume 1的NoCondition市价入场。持有反向仓位时，先提交数量1的ReduceOnly市价平仓；只有完全成交的平仓Order才会启动新方向上数量1的NoCondition市价入场。
- Combination方块把四个新仓动作的成交合并为一条冷却流。一次成交会禁止入场，随后十根已完成K线被跳过，第十一根已完成K线才是首次可再次决策的K线。图表没有独立离场、止损、止盈或持仓保护方块。

## 入场与出场规则

- **做多入场**: 任意两个做多投票一致且冷却就绪时，空仓路径提交一笔Order Volume 1的NoCondition市价买入。若当前为短仓，图表先提交数量1的ReduceOnly市价买入；只有该平仓Order完全成交后，才触发数量1的NoCondition市价买入。已有长仓保持不变。
- **做空入场**: 任意两个做空投票一致且冷却就绪时，空仓路径提交一笔Order Volume 1的NoCondition市价卖出。若当前为长仓，图表先提交数量1的ReduceOnly市价卖出；只有该平仓Order完全成交后，才触发数量1的NoCondition市价卖出。已有短仓保持不变。
- **离场**: 没有单独的离场规则。反方向的多数事件通过分阶段序列先关闭当前方向，再开立新方向。ReduceOnly平仓不会增加敞口，但两步都使用固定的Order Volume 1；该序列按图表建立的一单位持仓设计，因此实际持仓数量不同时，仓位可能无法完全平掉，也可能无法以信号方向结束。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles Series | 00:30:00 | 30分钟K线序列；只有已完成K线会更新指标、重置每根K线的多数Flag、推进冷却并启动决策。 |
| MACD Fast Length | 12 | 固定在MACD Indicator块内；如需更改快线EMA周期，请编辑该块。 |
| MACD Slow Length | 26 | 固定在MACD Indicator块内；如需更改慢线EMA周期，请编辑该块。 |
| MACD Signal Length | 9 | 固定在MACD Indicator块内；如需更改Signal EMA周期，请编辑该块；该线在一根K线内的斜率提供MACD投票。 |
| Stochastic K Length | 14 | 固定在Stochastic Indicator块内；如需更改%K周期，请编辑该块。做多和做空阈值分别为20和80。 |
| Stochastic D Length | 3 | 固定在Stochastic Indicator块内；如需更改%D周期，请编辑该块。方向投票读取%K，但完整指标必须已经形成。 |
| RSI Length | 14 | RSI周期。固定方向阈值为严格小于40和严格大于60。 |
| Cooldown Bars | 10 | 新仓成交后禁止入场的后续已完成K线数量；在第11根K线上恢复决策。 |
| Order Volume | 1 | 用于空仓入场、ReduceOnly平仓动作以及平仓单确认完全成交后入场的固定数量。 |

## 图表详情

- [K线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)方块只输出已完成的30分钟K线。三个只输出已形成值的[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)方块计算MACD 12/26/9、Stochastic 14/3和RSI 14。
- [转换器](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/converter.html)方块提取MACD Signal线。[Previous value](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)方块保留其前一根K线的值，严格比较把当前Signal归类为上升、下降或不变。
- 其他[比较](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)方块实现精确的固定振荡指标边界：Stochastic %K使用`≤ 20`和`≥ 80`，RSI使用`< 40`和`> 60`。这些阈值和两票要求都是图表的固定设置，不作为公开参数。
- 六个两两组合的[Logical condition](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)方块覆盖三个可能的做多组合和三个可能的做空组合。两个[Flag](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/flag.html)方块在每根K线上重置，把多个满足条件的组合归并为每个方向的一次多数事件。
- 当前[Position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html)和冷却就绪状态会为该K线周期创建快照。持仓比较区分空仓、长仓和短仓敞口，入场条件同时要求多数事件和可用的冷却状态。
- 六个[修改持仓](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)方块实现两个空仓入场和两个分阶段反转。空仓路径由外部`Position = 0`条件保护；反转平仓方块使用ReduceOnly，其完全成交的Order输出触发反方向的固定NoCondition入场。
- [Combination](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/combination.html)方块立即合并四个新仓方块的MyTrade输出，既不计数也不改变它们。合并后的成交会禁止入场并触发[N values](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)方块；它对随后十根已完成K线计数，并在第11根K线恢复就绪。[图表面板](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/chart.html)接收K线、三个数值信号流、合并后的新仓成交以及两个反转平仓成交。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
