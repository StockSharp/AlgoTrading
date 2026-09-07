# 斐波那契 SAR 回调限价策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该图表将两种速度的 Parabolic SAR 与三根蜡烛范围结合起来，每次只放置一张斐波那契回调限价单，在条件反转时撤销挂单，并按入场时保存的范围水平退出已成交仓位。

![schema](schema.svg)

## 策略概述

- 已完成的一小时 BTCUSDT 蜡烛同时输入快速和慢速 Parabolic SAR、Highest(3) 与 Lowest(3)。所有指标形成后才开始决策。
- 已形成的 Lowest 输出作为批次时钟；当前 Close、SAR、最高价、最低价、仓位和挂单状态全部锁存后，它只释放一次决策。
- 全局挂单锁保证同时最多只有一张有效入场单。订单进入终态时解除该锁；成交后，采样仓位会阻止再次入场。
- 入场、撤单和退出条件先写入静默评分锁存器，再于每根已完成蜡烛释放一次，避免混用相邻蜡烛的数据。
- 图表显示蜡烛、两条 SAR、范围和已保存的保护水平、已登记及已撤销的限价单、市价退出和全部成交。

## 入场和退出规则

- **多头入场**：当 `Slow SAR < Fast SAR < Close`、仓位为空且没有待处理入场时，以 `Low3 + (High3 - Low3) * 50%` 提交 Buy 限价单。若成交前出现 `Slow SAR > Fast SAR` 或 `Fast SAR >= Close`，则撤销该单。
- **空头入场**：当 `Slow SAR > Fast SAR > Close`、仓位为空且没有待处理入场时，以 `High3 - (High3 - Low3) * 50%` 提交 Sell 限价单。若成交前出现 `Slow SAR < Fast SAR` 或 `Fast SAR <= Close`，则撤销该单。
- **退出**：接受入场信号时保存对应方向的水平。多头止损为 `Low3 - 30`，目标为 `Low3 + (High3 - Low3) * 161%`；空头止损为 `High3 + 30`，目标为 `High3 - (High3 - Low3) * 161%`。已完成蜡烛的 Close 触及任一保存水平时，提交一张数量为 1 的反向市价单。

## 参数

| 参数 | 默认值 | 说明 |
|---|---|---|
| Security | BTCUSDT@BNBFT | 已完成蜡烛订阅所用的品种。订单和成交使用相同的 Strategy Security。 |
| Candle Series | 01:00:00 | 用于指标、决策、退出和图表的已完成一小时蜡烛。 |
| Fast SAR Acceleration | 0.02 | 快速 Parabolic SAR 的初始加速因子。 |
| Fast SAR Increment | 0.02 | 快速 Parabolic SAR 的加速增量。 |
| Fast SAR Maximum | 0.20 | 快速 Parabolic SAR 的最大加速因子。 |
| Slow SAR Acceleration | 0.01 | 慢速 Parabolic SAR 的初始加速因子。 |
| Slow SAR Increment | 0.02 | 慢速 Parabolic SAR 的加速增量。 |
| Slow SAR Maximum | 0.10 | 慢速 Parabolic SAR 的最大加速因子。 |
| High Lookback | 3 | Highest 计算 `High3` 使用的已完成蜡烛数量。 |
| Low Lookback | 3 | Lowest 计算 `Low3` 使用的已完成蜡烛数量。 |
| Entry Fibonacci, % | 50 | 限价在当前三蜡烛范围内的位置。 |
| Target Fibonacci, % | 161 | 每个已保存获利目标采用的范围倍数。 |
| Stop Offset | 30 | 已保存止损位于三蜡烛最低价或最高价之外的绝对价格距离。 |
| Order Volume | 1 | 每次入场和每次带方向保护的市价退出数量。 |

## 图表细节

- Security [Variable](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) 配置已完成的 [Candles](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)；交易模块使用 Strategy Security 和 Strategy Portfolio。
- 四个仅输出已形成值的 [Indicator](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 分别计算两条 Parabolic SAR 以及三根蜡烛的最高价和最低价。Lowest 输出是统一批次时钟。
- [Formula](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/formula.html)、Variable 和 [Comparison](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) 对齐数值输入，应用空仓及挂单方向保护，并且只发出为真的动作脉冲。
- 每个被接受的信号都会先保存计算出的止损和目标，再触发 [Order registering](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/orders/register.html)。仓位持有期间，这些保存值不会移动。
- 已登记订单的引用会保留给定向 [Order cancellation](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html)。只有登记模块在成交、确认撤单或登记失败后发出的 Finished 事件才能解除挂单锁。
- 带方向保护的 [Modify position](https://doc.stocksharp.com/zh/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) 在已完成 Close 触及保存的止损或目标时提交固定一单位反向市价单。图表接收所有相关价格、订单、撤单和 MyTrade 流。

## 使用方法

将 `.json` 文件导入 Designer，把 Strategy Security 设置为 BTCUSDT@BNBFT，并在一小时历史数据上运行。用于实盘前，请核对品种价格尺度、斐波那契水平、止损偏移、订单生命周期和市价退出行为。
