# 参考品种趋势策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图先同步 BTCUSDT@BNBFT 与 TONUSDT@BNBFT 的已完成五分钟 K 线，再在 TON 的 EMA 趋势确认同一方向时交易 BTC 的精确 EMA 交叉。仓位与冷却条件过滤每次决策，两条固定数量的市价单路径管理敞口。

![schema](schema.svg)

## 策略概览

- Sync 模块接收两路已完成五分钟 K 线，其 Interval 为 `00:05:00`，ClearSockets 已启用。只有两根 K 线都存在时才输出对齐的 BTC–TON 组合；缺少任意一根时，该不完整时段会被丢弃。
- 每个对齐组合随后输入 BTC 的快速 EMA 7、慢速 EMA 18，以及 TON 的快速 EMA 47、慢速 EMA 50。四个指标都关闭了仅输出已形成值的过滤。
- BTC 向上交叉要求 `PrevFast <= PrevSlow` 且 `Fast > Slow`；向下交叉要求 `PrevFast >= PrevSlow` 且 `Fast < Slow`。TON 的当前关系以 `Fast > Slow` 确认买入，以 `Fast < Slow` 确认卖出。
- 买入路径还要求 `Position <= 0`，卖出路径要求 `Position >= 0`。两条路径都以固定 Volume 1 提交 `NoCondition` 市价单。
- 最初五个同步的 BTC–TON 组合被阻止，每次订单信号还会阻止随后五个同步组合；第六个对齐组合重新具备资格。图中没有止损、止盈或独立离场模块，图表显示 BTC K 线、两条 BTC EMA 和两个成交流。

## 入场与出场规则

- **做多入场**: 当 BTC 满足 `PrevFast <= PrevSlow` 和 `Fast > Slow`，TON 当前满足 `Fast > Slow`，同步仓位检查为 `Position <= 0` 且冷却已结束时，图表以 Volume 1 提交市价买单。
- **做空入场**: 当 BTC 满足 `PrevFast >= PrevSlow` 和 `Fast < Slow`，TON 当前满足 `Fast < Slow`，同步仓位检查为 `Position >= 0` 且冷却已结束时，图表以 Volume 1 提交市价卖单。
- **离场**: 图中没有专用离场或保护模块。之后符合条件的反向订单会减少敞口；仓位为 `+1` 或 `-1` 时，固定 Volume 1 会将仓位归零，而不会建立反向仓位。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Main Fast EMA | 7 | 按已完成的五分钟 BTCUSDT@BNBFT K 线计算的快速 ExponentialMovingAverage 周期；仅输出已形成值的过滤已关闭。 |
| Main Slow EMA | 18 | 按已完成的五分钟 BTCUSDT@BNBFT K 线计算的慢速 ExponentialMovingAverage 周期；仅输出已形成值的过滤已关闭。 |
| Reference Fast EMA | 47 | 按对齐后的已完成五分钟 TONUSDT@BNBFT K 线计算的快速 ExponentialMovingAverage 周期；仅输出已形成值的过滤已关闭。 |
| Reference Slow EMA | 50 | 按对齐后的已完成五分钟 TONUSDT@BNBFT K 线计算的慢速 ExponentialMovingAverage 周期；仅输出已形成值的过滤已关闭。 |
| Cooldown Bars | 5 | 初始阶段及信号后被阻止的同步 K 线组合数量，经过这些组合后下一个对齐组合才具备资格。 |
| Volume | 1 | 传给两个 NoCondition 市价单模块的固定数量。 |

## 图表详情

- 两个 [K 线](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)模块把 BTCUSDT@BNBFT 与 TONUSDT@BNBFT 的已完成五分钟 K 线直接送入同步。
- [Sync](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/sync.html) 模块以 Interval `00:05:00` 和 ClearSockets `true` 对齐两个 K 线输入。它把两根 K 线作为一个组合输出；若缺少一侧，不完整时段会被清除，不进入指标链。
- 只有同步组合才会输入四个[指标](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)模块：BTC 的 ExponentialMovingAverage 7 和 18，以及 TON 的 47 和 50。它们的仅输出已形成值选项均为 `false`，只有两条 BTC EMA 输出还会发送到图表。
- [先前值](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)模块保留对齐后的 BTC 快速与慢速 EMA 上一值。[比较](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)模块表达每次精确交叉的两侧条件以及两个当前 TON 趋势关系；不同的[逻辑条件](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html)路径分别将其组合为买入和卖出条件。
- 当前[仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/current.html)提供 `Position <= 0` 与 `Position >= 0` 检查。[N values](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) 门控接收 Sync 输出的对齐 BTC K 线，并以 N=5 抑制最初五个同步组合以及每次订单信号后的五个组合，在第六个重新启用决策。
- 买入和卖出[修改仓位](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)模块使用 `NoCondition` 和共享 Volume 1 提交市价单。图中没有止损、止盈、仓位保护或独立离场元素。
- [图表面板](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/chart.html)接收已完成的 BTC K 线、BTC EMA 7、BTC EMA 18，以及买入与卖出订单模块的 MyTrade 输出。

## 使用方法

将 `.json` 文件导入 Designer，在回测器中用历史数据运行，然后根据自己的交易品种调整参数或方块，确认无误后再用于实盘。
