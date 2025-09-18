# Otkat Sys 策略

该策略复刻 MetaTrader 专家顾问 **1_Otkat_Sys**。它跟踪上一交易日的开盘价、收盘价、最高价和最低价，并在
每天午夜后的前三分钟（经纪商时间）从周二到周四判断是否入场。

## 交易逻辑

1. **日线统计** – 缓存上一根已完成的日线，用于计算：
   - `Open - Close` 与 `Close - Open`，判断上一交易日是空头还是多头。
   - `Close - Low` 与 `High - Close`，衡量价格距离极值的回撤幅度。
2. **进场窗口** – 仅当进场蜡烛的开盘时间位于 00:00 到 00:03 之间时才评估信号。周一与周五被跳过，以保持
   与原始 EA 的 `DayOfWeek` 过滤一致。
3. **方向过滤** – 四个互斥条件完整复刻 MQL 规则：
   - 若上一日收跌（`Open - Close` 高于通道阈值）且回撤较浅（`Close - Low` 低于 `Pullback - Tolerance`），则做多。
   - 若上一日收涨且向上回撤较深（`High - Close` 高于 `Pullback + Tolerance`），亦做多。
   - 若上一日收涨且向上回撤较浅（`High - Close` 低于 `Pullback - Tolerance`），则做空。
   - 若上一日收跌且向下回撤较深（`Close - Low` 高于 `Pullback + Tolerance`），则做空。
4. **下单** – 使用设定的手数直接以市价入场。多单的止盈距离为 `TakeProfit + 3` 个点（原版 EA 的写法），空单
   止盈为 `TakeProfit` 点，双向共用同一止损距离。
5. **时间止盈** – 若到 22:45 仍有持仓，则立即平仓，对应原程序在晚间强制平仓的逻辑。

所有阈值参数均以点数表示，并通过交易品种的 `PriceStep` 自动换算为价格距离。

## 参数

| 名称 | 说明 |
| --- | --- |
| `EntryCandleType` | 用于进场窗口的时间框架（默认 1 分钟）。 |
| `DailyCandleType` | 提供日线统计的数据类型（默认 1 天）。 |
| `TakeProfit` | 止盈点数，多单会额外加 3 点缓冲。 |
| `StopLoss` | 止损点数。 |
| `PullbackThreshold` | 基础回撤（“Otkat”）阈值。 |
| `CorridorThreshold` | 方向通道阈值（`KoridorOC`）。 |
| `ToleranceThreshold` | 回撤容差（`KoridorOt`）。 |
| `TradeVolume` | 每次入场的手数。 |

策略在 `Reset` 时清空缓存，订阅进场与日线的蜡烛流，并在可用的图表区域绘制蜡烛与成交标记。
