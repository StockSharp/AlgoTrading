# Universal MA Cross V4 策略

## 概述
**Universal MA Cross V4 策略** 是将 MetaTrader 4 专家顾问“Universal MACross EA v4”移植到 StockSharp 高级 API 的版本。策略监控可配置的快、慢移动平均线交叉，支持多种均线类型、价格来源、可选的交易时段过滤以及包含反手、止盈止损和跟踪止损的仓位管理。该实现基于蜡烛图订阅，在每根完成的 K 线结束时执行决策。

## 交易逻辑
### 指标处理
* 每根已完成的蜡烛都会计算两条移动平均线，每条均线都可以拥有自己的周期、平滑方法（简单、指数、平滑或线性加权）以及价格来源（收盘价、开盘价、最高价、最低价、中位价、典型价或加权价）。
* **MinCrossDistancePoints** 要求快线和慢线在产生交叉信号时至少相差指定的点数。启用 **ConfirmedOnEntry** 时，策略会在上一根完成的 K 线上验证交叉，复现原 EA 的“confirmed”模式。
* 设置 **ReverseCondition** 可以在不改变指标参数的情况下互换多空条件。

### 入场规则
1. 当快线向上穿越慢线并且差值不少于 **MinCrossDistancePoints** 时开多单；当快线向下穿越慢线并达到该差值时开空单。
2. 如果 **StopAndReverse** 为真，出现反向信号时会先平掉当前仓位，再评估新的入场机会。
3. **OneEntryPerBar** 选项通过记录最近一次下单的 K 线时间戳，阻止在同一根蜡烛内重复开仓。
4. 订单手数由 **TradeVolume** 参数控制，StockSharp 会把该值应用到市价单中。

### 仓位管理
* **StopLossPoints** 和 **TakeProfitPoints** 以点数定义止损与止盈距离，会依据标的的价格步长转换成绝对价格。当启用 **PureSar** 时，所有保护性逻辑（止损、止盈和跟踪止损）都会停用，与原版 EA 的 “Pure SAR” 模式保持一致。
* 跟踪止损模仿 MQL 的写法：价格相对入场价运行超过 **TrailingStopPoints** 时，止损会以相同距离跟随价格移动。启用 **PureSar** 时不执行跟踪。
* 每根完成的蜡烛都会检查止损和止盈。如果蜡烛的最高/最低价触及保护水平，策略会以市价平仓，以保证历史回测时的确定性。

### 时段过滤
* **UseHourTrade** 会把交易限制在 **StartHour** 与 **EndHour**（0–23，含端点）之间；若结束小时小于起始小时则视为跨越午夜。即便超出交易窗口，仓位管理（例如跟踪止损）仍会继续运行，但不会再开新单。

## 参数
| 参数 | 说明 |
|------|------|
| `FastMaPeriod`, `SlowMaPeriod` | 快、慢移动平均线的周期长度。 |
| `FastMaType`, `SlowMaType` | 移动平均线类型：简单、指数、平滑或线性加权。 |
| `FastPriceType`, `SlowPriceType` | 各均线使用的价格来源。 |
| `StopLossPoints`, `TakeProfitPoints` | 以点数表示的止损、止盈距离，设为 `0` 表示禁用。 |
| `TrailingStopPoints` | 以点数表示的跟踪止损距离，设为 `0` 表示关闭跟踪。 |
| `MinCrossDistancePoints` | 验证交叉时要求的最小均线差值。 |
| `ReverseCondition` | 互换多空条件。 |
| `ConfirmedOnEntry` | 在上一根完成的 K 线上确认信号，关闭后即时确认。 |
| `OneEntryPerBar` | 同一根 K 线最多只允许一次新开仓。 |
| `StopAndReverse` | 出现反向信号时先平仓再反向开仓。 |
| `PureSar` | 禁用止损、止盈和跟踪止损。 |
| `UseHourTrade`, `StartHour`, `EndHour` | 交易时段过滤设置。 |
| `TradeVolume` | 市价开仓时使用的订单手数。 |
| `CandleType` | 用于计算指标的蜡烛数据类型。 |

## 转换说明
* 所有与价格相关的距离均以 MetaTrader 点值提供，工具方法 `GetPriceOffset` 会根据证券的价格步长或小数位数转换为 StockSharp 实际价格，保证不同品种下的行为与原 EA 保持一致。
* 因为 StockSharp 的高级策略在完成的蜡烛上运行，跟踪止损在策略内部实现，以确保历史回测与实时运行的逻辑一致。
* 根据需求，本转换仅提供 C# 版本及多语言文档，不包含 Python 版本。
