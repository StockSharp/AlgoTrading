# EMA 交叉挂单阶梯策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表用两阶入场交易已确认的 EMA 交叉。市价单建立或完全反转仓位；市价单完全成交后，按最新 BestBid 在同一方向挂出更远的限价单；绝对价格距离保护管理所得敞口。100 根 K 线的冷却期会同时暂停新的第一阶入场和保护模块基于已完成收盘价的价格检查。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟 K 线进入仅输出已形成值的快速 EMA 14 和慢速 EMA 50。当快速 EMA 上穿慢速 EMA 时，Crossing 产生向上事件；下穿时产生向下事件。
- K 线时刻的仓位快照与就绪状态共同过滤入场。向上交叉仅在 Position <= 0 时允许买入，向下交叉仅在 Position >= 0 时允许卖出；第一阶市价单使用 Base Volume + abs(Position)，因此可从空仓开仓或完全反转相反敞口。
- 第一阶市价单达到 Matched 后，第二阶以持续采样并保存的最新 BestBid 为基准。多头分支按 BestBid - 100 提交买入限价单，空头分支按 BestBid + 100 提交卖出限价单；每单数量为 Base Volume 1，ShrinkPrice 关闭。
- 最近注册的第二阶订单会在未经过入场门控的反向 EMA 交叉、保护成交或冷却结束时撤销。第二阶成交会计入仓位保护，但不会重新启动冷却。
- 四个入场订单模块的成交都进入绝对价格距离保护，Take Distance 为 400，Stop Distance 为 200。就绪状态有效时，每个已完成收盘价都会接受检查，触发的退出以市价提交。第一阶成交或保护退出会启动冷却：随后 100 根已完成 K 线既不允许新的第一阶入场，也不执行保护价格检查；两者都在第 101 根恢复。图表显示 K 线、两条 EMA、两个限价 Order 流和五个成交流。

## 入场与出场规则

- **做多入场**: 快速 EMA 上穿慢速 EMA 时，若仓位快照小于等于零且冷却已就绪，该图表按 Base Volume + abs(Position) 市价买入。订单完全成交后，再按保存的 BestBid 减去 Rung Distance，以 Base Volume 提交买入限价单。
- **做空入场**: 快速 EMA 下穿慢速 EMA 时，若仓位快照大于等于零且冷却已就绪，该图表按 Base Volume + abs(Position) 市价卖出。订单完全成交后，再按保存的 BestBid 加上 Rung Distance，以 Base Volume 提交卖出限价单。
- **离场**: 仓位保护接收两个市价阶和两个限价阶的成交。就绪状态有效时，它检查已完成 K 线的收盘价；价格达到有利的 400 个价格单位或不利的 200 个价格单位时以市价退出。第一阶成交或保护退出后的 100 根已完成 K 线不执行保护价格检查，第 101 根恢复检查。保护成交会撤销最后一个等待中的第二阶；未经过入场门控的反向交叉也会独立撤销该挂单。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Candles | 00:05:00 | 五分钟周期；只有已完成 K 线驱动 EMA 计算、信号、保护检查和冷却计数。 |
| Fast EMA Length | 14 | 快速 ExponentialMovingAverage 的长度；仅输出已形成值。 |
| Slow EMA Length | 50 | 慢速 ExponentialMovingAverage 的长度；仅输出已形成值。 |
| Base Volume | 1 | 第一阶市价单中加到 abs(Position) 的数量；第二阶限价单直接使用该数量。 |
| Rung Distance | 100 price units | 相对已保存 BestBid 的绝对价格偏移：买入限价单做减法，卖出限价单做加法。 |
| Cooldown | 100 candles | 同时禁止新第一阶入场和保护价格检查的后续已完成 K 线数量；两者都在第 101 根 K 线恢复。 |
| Take Distance | 400 price units | 触发保护性市价退出的绝对有利价格变动。 |
| Stop Distance | 200 price units | 触发保护性市价退出的绝对不利价格变动。 |

## 图表详情

- [K 线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)模块只输出已完成的五分钟 K 线。两个仅输出已形成值的[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)模块计算 ExponentialMovingAverage 14 和 50。
- [交叉](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)对向上事件输出 true、对向下事件输出 false；NOT [逻辑条件](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)使向下事件可用于操作。[仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html)在 EMA 路径之前取样，[比较](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)模块把 Position <= 0 或 Position >= 0 与就绪状态组合。Long 与 Short 入场门仅把 true 脉冲传给第一阶触发器。
- [公式](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html)计算 Base Volume + abs(Position)。第一阶[订单注册](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/register.html)模块提交 NoCondition 市价单，其 Matched 输出触发对应的第二阶。
- 持续运行的 [Level1](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html) 模块提供 BestBid，并由[变量](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)保存。价格[公式](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html)模块计算 BestBid - Rung Distance 和 BestBid + Rung Distance；第二阶[订单注册](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/register.html)模块以 Base Volume 提交同向限价单，ShrinkPrice 为 false。
- 每个新的第二阶都成为[撤销订单](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html)所保存的订单。另一方向的直接交叉、保护成交或冷却完成都会触发撤单。限价阶成交加入受保护敞口，但不触发冷却。
- [仓位保护](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)接收四个入场模块的成交，并在提交市价退出前使用绝对止盈与止损距离。只有就绪状态有效时，保存的已完成 K 线收盘价才会送出进行价格检查。[N 个值](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)模块和状态变量在第一阶市价成交或保护退出后，精确禁止随后 100 根已完成 K 线的第一阶入场和这些检查，并在第 101 根同时恢复两者。
- [图表面板](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/chart.html)接收已完成 K 线、快速 EMA 14、慢速 EMA 50、买入限价与卖出限价的 Order 流，以及五个 MyTrade 流：市价买入、市价卖出、限价买入、限价卖出和保护退出。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
