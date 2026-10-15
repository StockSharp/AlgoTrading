# 权益回撤监控
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表根据策略盈亏构建权益曲线、保存持续峰值、对每次新的回撤阈值突破仅记录一次日志，并运行特意稀疏的受保护多头周期，使回测期间被监控的账户数值产生变化。

![schema](schema.svg)

## 策略概览

- 已完成的五分钟 BTCUSDT K 线提供唯一的采样时钟。并行的 Level 1 最佳买价订阅在 K 线采样之间持续更新未实现盈亏估值。
- P&L change 按自身事件频率更新静默的已实现与未实现盈亏存储。每根已完成 K 线将两个最新值和 Start Balance 各释放一次，计算 `Equity = Start Balance + Realized P&L + Unrealized P&L`。
- `max(已存峰值, 权益)` 维护整个运行期间的权益峰值。仅输出已形成结果的 Highest(2) 确认该单调峰值流，并加入一次采样的预热，而不会缩短峰值历史。
- 回撤公式为 `(Peak - Equity) / Peak * 100`。Comparison 验证 `Drawdown >= Drawdown Alert`，Crossing 与布尔存储仅将新的向上阈值穿越送入日志。
- Modify position 从空仓状态建立一个市价多头仓位。绝对距离保护负责平仓，1,440 根 K 线的计时器仅在后续五天的五分钟 K 线结束后允许再次入场。

## 入场与出场规则

- **多头入场**：Highest(2) 形成后，可用的入场 Flag 将 Volume 1 送入设置为 Buy、OpenPosition 和 MarketOrder 的 Modify position 模块。因此第一次入场尝试发生在第二根已完成 K 线上。
- **空头入场**：该图表不建立空头仓位。Sell 成交仅用于退出受保护的多头仓位。
- **出场**：价格向有利方向移动 0.04 或向不利方向移动 0.03 个绝对价格单位后，Position protection 提交市价退出。入场成交启动后续 1,440 根 K 线的冷却；重置入场就绪状态的是计时器，而不是出场成交。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Candles、Level 1 与 Strategy trades 订阅使用的品种。交易和盈亏计算的 Strategy Security 应设置为同一品种。 |
| Candle Series | 00:05:00 | 已完成 K 线周期，同时作为权益采样和冷却步进的时钟。 |
| Start Balance | 1000 | 计算权益时加到已实现和未实现盈亏上的基准金额。 |
| Peak Confirmation Length | 2 | 单调持续峰值流的 Highest 长度；交易会延迟到第二次采样。 |
| Drawdown Alert, % | 1 | 当回撤从下方向上达到或超过该水平时写入日志。 |
| Volume | 1 | 每次多头入场的数量。 |
| Entry Cooldown N | 1440 | 两次允许入场之间需经过的后续五分钟已完成 K 线数量，等于五天。 |
| Take Distance | 0.04 | 触发市价平多的有利绝对价格距离。 |
| Stop Distance | 0.03 | 触发市价平多的不利绝对价格距离。 |

## 图表细节

- BTC [Variable](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) 向已完成 [Candles](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)、最佳买价 [Level 1](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html) 和 [Strategy trades](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) 提供品种。交易模块使用 Strategy Security 与 Strategy Portfolio。
- 静默 Variable 存储将事件驱动的 [P&L change](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) 流与 K 线时钟分离。固定的释放顺序让每个 [Formula](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html) 都获得同一根 K 线的一组完整输入。
- 持续峰值从零开始，因此修改 Start Balance 仍能得到正确结果。max Formula 先更新该状态，再将单调输出送入仅输出已形成结果的 [Indicator](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) Highest(2)。
- [Comparison](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 与 [Crossing](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) 按固定顺序接收阈值和回撤。恢复时的 false 穿越被忽略；向上的 true 穿越通过 [String Formatter](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) 将保存的百分比送入 Log [Notification](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html)。
- 冷却 [Delay](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) 模块在同一根 K 线的任何决策之前接收该 K 线。Buy 成交启动 N = 1440，其输出会在符合条件的 K 线到达 Highest 之前重置入场 Flag。
- [Modify position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 的 Buy 成交初始化局部 [Position protection](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/protect.html)。图表显示 K 线、采样权益、峰值、回撤、两项盈亏成分、入场成交、保护成交和全部策略成交。

## 使用方法

将 `.json` 文件导入 Designer，把 Strategy Security 设置为 BTCUSDT@BNBFT，并在内置的三月历史数据上运行。用于实盘交易前，请针对所用品种核对权益尺度、绝对保护距离、警报百分比和五天冷却时间。
