# 三根阴线与定时退出策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图在波动率升高时交易连续三根同色蜡烛。连续三根阴线且 ATR 14 高于其 30 个值均线的 0.8 倍时形成做多条件；连续三根阳线在相同波动率规则下形成做空条件。空仓时直接开仓，反向持仓通过成交确认的两步流程反转，现有持仓也可在相反三蜡烛形态出现或经过二十根已完成蜡烛后平仓。每次操作序列都会启动十二根蜡烛的冷却期。

![schema](schema.svg)

## 策略概览

- 仅处理已完成的 30 分钟蜡烛。两个 Previous value 和六个 Converter 提取当前蜡烛及前两根蜡烛的 Open 与 Close，因此每根蜡烛都会执行滚动的三阴线和三阳线检查。
- ATR 14 及其 30 值简单均线都必须形成。高波动率采用严格条件 `ATR > ATR 均线 × 0.8`；指标预热期间不作交易决定。
- 满足条件的三阴线从空仓买入或反转空头；满足条件的三阳线从空仓卖出或反转多头。反转时先用 ReduceOnly 平掉一单位，只有平仓 Order 完全成交后才在新方向开一单位。
- 若不满足高波动率反转条件，三阳线平多，三阴线平空。状态计数器还会在持仓达到二十根已完成蜡烛时平仓；同一根蜡烛上的反转优先于定时退出。
- 三个 Combination 分别合并两个多头退出原因、两个空头退出原因以及八个交易动作的成交流。成交后接下来的十二根已完成蜡烛禁止新动作，第十三根才可再次决策。图中没有止损、止盈或持仓保护块。

## 入场与出场规则

- **做多入场**: 当当前蜡烛及前两根已完成蜡烛都收于开盘价下方、ATR 14 严格高于 `ATR 均线 × 0.8` 且冷却已就绪时，空仓提交 Order Volume 1 的 NoCondition 市价买单。若持有空头，图先提交数量 1 的 ReduceOnly 市价买单；完全成交的 Order 刷新数量值并触发数量 1 的 NoCondition 市价买单。
- **做空入场**: 当当前蜡烛及前两根已完成蜡烛都收于开盘价上方、ATR 14 严格高于 `ATR 均线 × 0.8` 且冷却已就绪时，空仓提交 Order Volume 1 的 NoCondition 市价卖单。若持有多头，图先提交数量 1 的 ReduceOnly 市价卖单；完全成交的 Order 刷新数量值并触发数量 1 的 NoCondition 市价卖单。
- **离场**: 当连续三根阳线没有同时形成高波动率反转，或 Max Hold Bars 达到 20 时平多；空头则在连续三根阴线或持有 20 根蜡烛后对称平仓。每个方向的形态与计时器事件先合并，Flag 保证每根蜡烛最多执行一次独立平仓。每次独立平仓以及分步反转的两次成交都进入共同冷却流。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles Series | 00:30:00 | 30 分钟蜡烛序列；只有已完成蜡烛会更新形态、指标、计数器、冷却状态和交易决定。 |
| ATR Length | 14 | 仅输出已形成值的 Average True Range 指标周期。 |
| ATR Average Length | 30 | ATR 值的简单移动平均周期；该均线形成后才开始决策。 |
| ATR Multiplier | 0.8 | ATR 均线乘数。只有当前 ATR 严格高于所得阈值时，波动率才合格。 |
| Max Hold Bars | 20 | 持仓非零时累计的已完成蜡烛数，达到该值后允许定时平仓。 |
| Cooldown Bars | 12 | 一次操作序列后被阻止的后续已完成蜡烛数；第 13 根恢复决策。 |
| Order Volume | 1 | 空仓开仓、ReduceOnly 平仓以及平仓成交后的新开仓使用的固定数量；该图按自身建立的一单位持仓设计。 |

## 图表详情

- [蜡烛](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)块输出已完成的 30 分钟蜡烛。两个 [Previous value](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) 保留偏移 1 和 2，六个 [Converter](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/converter.html) 提取三组 Open/Close。
- 六个 [Comparison](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 使用严格的 `Close < Open` 与 `Close > Open` 分类每根蜡烛。两个三输入 [Logical condition](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) 形成滚动的阴线与阳线形态；十字星会使两种形态都为假。
- 两个仅输出形成值的 [Indicator](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 计算 ATR 14 和 ATR 的 SMA 30。[Formula](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html) 将均线乘以 0.8，严格比较产生高波动率标志。
- 当前 [Position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html) 每根蜡烛捕获两次：一次用于交易路由，一次用于持有计数器。[Variable](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) 与 Formula 只在持仓非零时递增计数，并在确认的动作成交后重置。
- 两个布尔 [Combination](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/combination.html) 合并形态与计时退出而不计数或修改值。每个方向的 [Flag](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/flag.html) 防止重复独立平仓，优先级门在同一蜡烛已满足反转时抑制定时或形态平仓。
- 八个 [Modify position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 实现两个空仓开仓、两个独立退出和两个分步反转。每次反转执行 `ReduceOnly close 1 → fully matched Order → NoCondition open 1`；已成交 Order 还会在同一事件周期重新输出数量。
- MyTrade Combination 将每次动作成交发送到 [N values](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) 冷却。第一次成交启动十二根蜡烛计数，同一反转的第二步不会重启正在运行的计数。[Chart panel](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/chart.html) 接收蜡烛、ATR、ATR 均线、阈值和所有动作成交。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
