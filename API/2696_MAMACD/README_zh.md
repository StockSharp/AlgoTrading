# MAMACD 策略

## 概述
本策略是在 StockSharp 高级 API 中对 MetaTrader 5 专家顾问 **MAMACD（barabashkakvn 版本）**（位于 `MQL/19334` 目录）的逐句移植。策略通过两条基于最低价的线性加权均线（LWMA）识别趋势通道，配合一条基于收盘价的快速 EMA 触发线，并使用 MACD 主线过滤信号。所有计算都在蜡烛完成之后执行，同时保留原始 EA 的“准备”标志机制——只有当快速 EMA 重新穿越 LWMA 通道后才允许新的交易。

## 指标
- **LWMA #1（最低价，默认 85）**：慢速趋势基准。
- **LWMA #2（最低价，默认 75）**：略快的确认均线，用于构建通道。
- **EMA 触发线（收盘价，默认 5）**：必须重新穿越两条 LWMA 才会激活买入或卖出信号。
- **MACD 主线（快速 15，慢速 26）**：趋势确认，做多需要 MACD 为正或向上，做空需要 MACD 为负或向下。

## 入场逻辑
1. 仅在蜡烛状态为 `CandleStates.Finished` 时处理，忽略未完成数据。
2. 当 EMA 触发线跌破两条 LWMA 时，设置“多头准备”标志；只有在 EMA 返回到两条 LWMA 之上且 MACD > 0 或者高于上一根值时，才会开多，并且同一时间只允许持有一张多单。
3. 当 EMA 触发线上穿两条 LWMA 时，设置“空头准备”标志；只有在 EMA 回落到 LWMA 之下且 MACD < 0 或者低于上一根值时，才会开空，并且同一时间只允许持有一张空单。
4. 成交量来自策略的 `Volume` 属性；当信号反向时，先平掉原有仓位，再建立新的方向。

## 出场逻辑
- 原版 EA 不包含主动平仓条件。这里通过 `StartProtection` 配置的止损与止盈（单位为“点”）来保护仓位，只要任一保护触发即可自动平仓。

## 参数
| 名称 | 说明 |
| --- | --- |
| `FirstLowMaLength` | 第一条 LWMA 的周期（最低价，默认 85）。 |
| `SecondLowMaLength` | 第二条 LWMA 的周期（最低价，默认 75）。 |
| `TriggerEmaLength` | 快速 EMA 触发线的周期（收盘价，默认 5）。 |
| `MacdFastLength` | MACD 快速 EMA 周期（默认 15）。 |
| `MacdSlowLength` | MACD 慢速 EMA 周期（默认 26）。 |
| `StopLossPips` | 止损距离（点）；0 表示禁用（默认 15）。 |
| `TakeProfitPips` | 止盈距离（点）；0 表示禁用（默认 15）。 |
| `CandleType` | 处理的蜡烛类型/时间框架（默认 1 小时）。 |

## 实现细节
- “点值”由 `Security.PriceStep` 推导而来；若价格精度为 3 位或 5 位，则自动乘以 10，以匹配 MT5 中对“点”的定义。
- MACD 缓冲区与 EA 一致：先记录第一条有效数据，再用作下一根蜡烛的比较基准。
- `_readyForLong` 与 `_readyForShort` 标志完全复刻原始 `startb` / `starts` 状态机，确保 EMA 必须离开 LWMA 通道后才允许再次入场。
- 代码会绘制价格与三条均线，并为 MACD 创建单独图层，方便验证移植结果。

## 映射关系
| MT5 元素 | StockSharp 对应实现 |
| --- | --- |
| `iMA`（最低价/收盘价） | `WeightedMovingAverage`（最低价输入）与 `ExponentialMovingAverage`（收盘价输入） |
| `iMACD` 主线 | `MovingAverageConvergenceDivergence` 主输出 |
| `buy` / `sell` 计数 | 通过 `Position` 的正负与 `BuyMarket` / `SellMarket` 控制仓位 |
| Magic number 与滑点 | 在高级 API 中无需显式处理 |
| 点数止损/止盈 | `StartProtection` + 由点值换算得到的绝对价格偏移 |

该实现忠实还原 MT5 版本的交易逻辑，并利用 StockSharp 的订阅、指标绑定以及风控工具实现结构化管理。
