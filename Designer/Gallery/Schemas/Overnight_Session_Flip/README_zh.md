# Overnight Session Flip 策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图将已形成的 20 周期 SMA 与两个定时市价单时段结合起来。策略时钟会检查最新的已完成五分钟蜡烛、持仓和日历日期：满足条件的买单可在 20 时内发送，满足条件的卖单可在 8 时内发送。日期锁存器把每个日历日期的已提交订单限制为一笔。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟蜡烛更新收盘价和周期为 20 的 SimpleMovingAverage。
- SMA 模块只输出已形成的值，因此定时决策会等待指标积累足够的蜡烛历史。
- Time 模块是决策时钟。每个时钟节拍先释放最新的收盘价、SMA 和持仓快照，再提供条件所用的小时与日历组成部分。
- 两个市价单模块都使用固定数量 1，且不设置持仓修改条件。持仓过滤器只允许在持仓小于等于零时买入，在持仓大于等于零时卖出。
- 数字日历日期键会在订单模块触发前被锁存。图表显示蜡烛、SMA 值和两个 MyTrade 流；该图不包含保护模块或单独的退出模块。

## 入场与出场规则

- **做多入场**: 在 Night Hour 20 内，最新已完成蜡烛的收盘价高于已形成的 SMA，当前持仓小于等于零，并且当前日历日期尚未提交订单。该图以市价买入 Volume 1。
- **做空入场**: 在 Day Hour 8 内，最新已完成蜡烛的收盘价低于已形成的 SMA，当前持仓大于等于零，并且当前日历日期尚未提交订单。该图以市价卖出 Volume 1。
- **离场**: 没有专用退出单或保护单。以后满足条件的反向固定数量订单可能缩减现有持仓、平掉数量相等的反向持仓，或在当前持仓绝对值小于 Volume 时越过零；它不保证完成全部反转。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | 五分钟蜡烛周期；只有已完成蜡烛才会更新价格和 SMA 快照。 |
| SMA Period | 20 | SimpleMovingAverage 使用的已完成蜡烛数量；决策要求 SMA 已形成。 |
| Night Hour | 20 | 买入条件可以提交订单的策略时钟小时。 |
| Day Hour | 8 | 卖出条件可以提交订单的策略时钟小时。 |
| Volume | 1 | 同时提供给两个市价单模块的固定数量。 |

## 图表详情

- [蜡烛](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)流进入收盘价[转换器](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)和只输出已形成 SMA 的[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)。表达式为 `a` 的[公式](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html)把 SMA 表示为数值。
- [当前时间](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/time/current_time.html)提供策略或消息时间戳。每个时钟节拍都会触发保存最新收盘价、SMA 和持仓值的[变量](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)，因此 Time 直接参与每次决策。
- 时间转换器提取 Hour、Year 和 DayOfYear。日期[公式](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html)计算 `Year * 1000 + DayOfYear`，为每个日历日期生成稳定键。
- [比较](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)模块检查两个计划小时、收盘价与 SMA、持仓与零，以及当前日期键与上次锁存键。[逻辑条件](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)模块分别组合买入和卖出条件。
- 每个时钟节拍都会采样当前[持仓](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html)。买入侧要求 `Position <= 0`，卖出侧要求 `Position >= 0`。
- 组合条件为真时，流程先锁存当前日期键，再触发对应的[修改持仓](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)模块。这个顺序会阻止同一日历日期后续时钟节拍再次提交订单。
- 两个 Modify position 模块接收共享的固定 Volume 值并放置市价单。Chart 面板接收已完成蜡烛、已形成的 SMA 流以及买卖模块的 MyTrade 输出。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
