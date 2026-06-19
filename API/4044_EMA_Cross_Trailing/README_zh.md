# EMA Cross Trailing 策略

## 概述
该策略是 MetaTrader 4 智能交易系统 `MQL/8606/EMA_CROSS_2.mq4` 的 StockSharp 版本。它保留了原始 EA 的核心思想：监控慢速与快速指数移动平均线的相对位置，并在出现交叉时开立单一仓位。止盈、止损以及跟踪止损全部交由高层 API `StartProtection` 处理，从而在 StockSharp 框架内复现原策略的退出管理。

## 交易逻辑
- 根据参数 `CandleType` 构建蜡烛（默认 15 分钟），并计算两条 EMA：`SlowEmaLength` 为慢速 EMA 周期，`FastEmaLength` 为快速 EMA 周期。
- 记录慢速 EMA 相对快速 EMA 的最近方向。与原始脚本中的 `first_time` 标志类似，指标完全形成后的第一根完结蜡烛只用于初始化方向，不触发交易。
- 当慢速 EMA 上穿快速 EMA（方向变为 `1`）且当前没有持仓时，发送市价买单；当慢速 EMA 下穿快速 EMA（方向变为 `2`）且仓位为空时，发送市价卖单。这一判定严格对应 MQL 函数 `Crossed(LEma, SEma)` 的返回值。
- 策略始终保持单仓模式：若已有仓位或入场单仍在等待成交，则忽略后续交叉信号。

## 风险与仓位管理
- `StartProtection` 根据标的的 `PriceStep` 计算价格距离，分别设置止盈、止损以及可选的跟踪止损。将 `TrailingStopPips` 设为 `0` 可以关闭跟踪功能。
- 入场采用 `BuyMarket`/`SellMarket` 市价单；当任一保护条件触发时，头寸会通过市价退出，这与原 EA 中 `OrderSend` + `OrderModify` 的行为一致。
- `OrderVolume` 控制基础下单手数。在每次下单前，系统会自动根据交易品种的最小/最大手数及手数步长对该值进行对齐，避免被交易所拒单。

## 参数
| 参数 | 说明 |
|------|------|
| `TakeProfitPips` | 止盈距离（以价格步长为单位），默认 20。 |
| `StopLossPips` | 止损距离（以价格步长为单位），默认 30。 |
| `TrailingStopPips` | 跟踪止损距离（以价格步长为单位），设为 `0` 表示禁用，默认 50。 |
| `OrderVolume` | 下单基准手数（在对齐之前），默认 2。 |
| `FastEmaLength` | 快速 EMA 的周期，默认 5。 |
| `SlowEmaLength` | 慢速 EMA 的周期，默认 60。 |
| `CandleType` | 构建蜡烛的时间框架，默认 15 分钟。 |

## 说明
- 原脚本中的 `Bars < 100` 检查被替换为“等待两条 EMA 均已形成”，在不牺牲稳定性的前提下更加符合 StockSharp 的工作流。
- 跟踪止损完全由保护模块自动调整，因此无需像 MQL 版本那样循环调用 `OrderModify`。
- 根据任务要求，本目录仅包含 C# 实现，暂不提供 Python 版本。
