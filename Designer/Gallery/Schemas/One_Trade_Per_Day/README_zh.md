# 每 24 小时一次交易策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表在已完成的四小时 K 线上交易 EMA(10) 与 EMA(30) 的严格交叉，并采用反向方向。可配置的交易时段门控决定候选信号何时有效，Flag 与计数六根 K 线的 N values 在每个滚动 24 小时间隔内最多放行一次入场决策。

![schema](schema.svg)

## 策略概览

- 已完成的四小时 K 线输入 EMA 10 和 EMA 30。交叉状态从计算开始就持续更新，但只有完成十根 K 线后才允许入场候选信号。
- 快 EMA 严格向上穿越慢 EMA 时产生卖出候选；严格向下穿越时产生买入候选。
- Time 与 Working time 只在配置的时段内放行候选信号。Combination 合并两个方向流，Flag 在重置前只释放第一个有效候选。
- 获准入场会启动 N values。再完成六根四小时 K 线后，计数器重置 Flag，从而形成滚动 24 小时限制。
- 空仓时，Position modify 提交一张固定 Volume 的市价单。持有相反方向的单位仓位时，它先平仓，再以第二张固定 Volume 市价单开立新方向。
- Position protection 是离场机制。它跟踪入场和反转的直接成交，并可在 3% 止盈或 2% 固定止损处平仓。

## 入场与出场规则

- **做多入场**: EMA 严格向下交叉、已完成十根预热 K 线、Working time 门控开启且滚动锁存可用时，买入 Volume。空仓时开立多仓；持有单位空仓时先买入一次平仓，再买入一次开立多仓。
- **做空入场**: EMA 严格向上交叉、已完成十根预热 K 线、Working time 门控开启且滚动锁存可用时，卖出 Volume。空仓时开立空仓；持有单位多仓时先卖出一次平仓，再卖出一次开立空仓。
- **离场**: Position protection 在 3% 止盈或 2% 固定止损处平掉所跟踪仓位。之后符合条件的反向交叉也可以先平仓、再开立新仓，从而完成反转。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 04:00:00 | 四小时时间框架；只有已完成 K 线驱动 EMA、预热、滚动计数和保护价格检查。 |
| Fast EMA Length | 10 | 快速指数移动平均线的周期。 |
| Slow EMA Length | 30 | 慢速指数移动平均线的周期。 |
| Warmup Bars | 10 | 允许交叉入场前需要完成的 K 线数量。 |
| Session From | 00:00:00 | 以策略回放时间或服务器时间表示的有效交易时段起点。 |
| Session Until | 23:59:59 | 以策略回放时间或服务器时间表示的有效交易时段终点。 |
| Rolling Cooldown Bars | 6 | 一次获准入场后，到下一次允许入场决策之间计数的已完成 K 线数量；六根四小时 K 线等于 24 小时。 |
| Volume | 1 | 每次开仓或反转动作使用的固定数量。 |
| Take Profit % | 3 | Position protection 使用的有利方向百分比变动。 |
| Stop Loss % | 2 | 固定、非跟踪止损使用的不利方向百分比变动。 |

## 图表详情

- [K 线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)方块把已完成四小时 K 线发送给两个[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)方块。Crossing 识别 EMA 10 与 EMA 30 的相对位置变化；前值与当前值的严格比较确认事件，NOT 分支生成向下交叉脉冲。
- 十根 K 线预热门控阻止早期候选。[Time](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/time.html)把回放或服务器时间交给 [Working time](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/working_time.html)；随附历史数据以 UTC 回放。
- Combination 将可执行的买卖候选传给 [Flag](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/flag.html)。第一个 true 候选被释放并启动 [N values](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/n_values.html)，后续候选会被锁定，直到再完成六根 K 线后重置 Flag。
- 当前仓位选择 [Position modify](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 路径。空仓入场使用一次固定方向市价动作；反转使用两次同方向的连续动作，先归零，再开仓。因此限制针对获准入场决策，而一次决策可以有意产生两次成交。
- 所有开仓和反转的直接成交都会更新 [Position protection](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)。已完成 K 线收盘价驱动价格检查；保护自身的平仓成交不会反馈到其交易输入。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
