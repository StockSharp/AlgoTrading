# MA + RSI 巫师策略

## 概述

该策略是 MetaTrader 5 中 `MQL/17489` 文件夹内 "MQL5 Wizard MA RSI" 专家的 StockSharp 版本。原始 EA 将移动平均线过滤器与 RSI 过滤器结合，通过加权得分跨越阈值时开平仓。C# 改写后的策略保持相同结构，同时利用 StockSharp 的高级 API 与风控工具。

策略适用于任意提供 OHLCV 蜡烛数据的交易品种。它计算一个可按用户设定周期与平移的移动平均线，以及一个可选择价格源的 RSI。两个指标共同构成一个复合得分；当得分超过开仓阈值时建立仓位，当反向得分达到平仓阈值时离场。额外的距离、止损、止盈设置复刻了原 EA 中的资金管理。

## 指标与得分

* **移动平均线**：周期、算法（简单、指数、平滑、线性加权）、价格源与平移量均可配置。收盘价高于平移后的均线时得分为 100，否则为 0。
* **相对强弱指标（RSI）**：周期与价格源可配置。RSI 从 50 上升至 100 的区间内，长方向得分线性增长到 100；当 RSI 低于 50 时，短方向得分同样线性增长。
* **复合得分**：使用 `MaWeight` 与 `RsiWeight` 对两个指标得分做加权平均 `score = (maScore * MaWeight + rsiScore * RsiWeight) / (MaWeight + RsiWeight)`，保证结果保持在 0 到 100 之间，与原版保持一致。
* **价格距离过滤**：`PriceLevelPoints` 指定收盘价与平移均线之间的最小距离（按价格步长转换）。距离不足的信号被忽略。

## 交易规则

1. 仅在蜡烛收盘时更新指标与得分。
2. 当反向得分超过 `ThresholdClose` 时，立即市价平掉现有仓位。
3. **做多**：在当前没有多头敞口的情况下，当多头得分 ≥ `ThresholdOpen`、冷却期 (`ExpirationBars`) 已结束并满足距离过滤时，按 `Volume + |Position|` 的数量下多单，可自动反手空头。
4. **做空**：逻辑与做多对称。
5. `StartProtection` 根据点数参数设置止损与止盈。

## 风险控制

策略启动后立即调用 `StartProtection`。`StopLevelPoints` 与 `TakeLevelPoints` 按价格点数定义，并使用当前品种的 `Security.PriceStep` 转换为实际价格。设置为 0 可禁用相应保护。`ExpirationBars` 充当同向再次开仓前的冷却时间，对应原 EA 中挂单过期的概念。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 分析使用的蜡烛类型。 | 15 分钟 K 线 |
| `ThresholdOpen` | 开仓所需的最小加权得分。 | 55 |
| `ThresholdClose` | 平仓所需的反向得分。 | 100 |
| `PriceLevelPoints` | 价格与平移均线的最小距离（点）。 | 0 |
| `StopLevelPoints` | 止损距离（点）。 | 50 |
| `TakeLevelPoints` | 止盈距离（点）。 | 50 |
| `ExpirationBars` | 同向再次开仓的冷却周期（根）。 | 4 |
| `MaPeriod` | 移动平均线周期。 | 20 |
| `MaShift` | 均线平移的蜡烛数量。 | 3 |
| `MaMethod` | 均线算法（Simple、Exponential、Smoothed、LinearWeighted）。 | Simple |
| `MaAppliedPrice` | 均线使用的价格。 | Close |
| `MaWeight` | 均线得分的权重。 | 0.8 |
| `RsiPeriod` | RSI 周期。 | 3 |
| `RsiAppliedPrice` | RSI 使用的价格。 | Close |
| `RsiWeight` | RSI 得分的权重。 | 0.5 |

## 说明

* 策略只处理已完成的蜡烛，忽略实时形成中的数据。
* 当两个权重同时为 0 时，将不会再有信号触发。
* `ExpirationBars` 设为 0 时，允许在同一方向上连续进场。
* 由于 StockSharp 默认使用市价单，原 EA 的挂单过期逻辑在此版本中通过冷却机制体现。
