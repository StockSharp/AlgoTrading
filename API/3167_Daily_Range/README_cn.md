# Daily Range 策略

## 概览
该策略是 MetaTrader 5 专家顾问 `MQL/23334/Daily range.mq5` 的 StockSharp 移植版本。原始 EA 通过统计最近几天的最高价和最低价，根据每日波动区间设置偏移量并交易突破。本 C# 实现保留了核心思想，同时利用 StockSharp 的高级策略 API。

## 策略逻辑
### 区间计算
* 策略为每个交易日保存最高价、最低价以及最后的收盘价。
* 维护一个包含 `SlidingWindowDays` 个最近交易日（含当日）的滑动窗口。
* `RangeMode` 用于选择区间的计算方式：
  * **HighestLowest**：窗口内最高点与最低点的距离。
  * **CloseToClose**：窗口内相邻交易日收盘价的绝对变化均值。
* 当新交易日达到预设的 `StartTime` 时，重新计算突破上下轨：
  * `Upper = Highest + Range × OffsetCoefficient`
  * `Lower = Lowest − Range × OffsetCoefficient`
* 在 `StartTime` 之前，继续沿用前一日的突破水平，以保持与原始 EA 相同的行为。

### 入场条件
* 当处理的蜡烛收盘价大于或等于上轨，且当日多头开仓次数少于 `MaxPositionsPerDay` 时开多。
* 当收盘价小于或等于下轨，且当日空头开仓次数未达到上限时开空。
* 如果从已有仓位反手，先平掉现有数量，再额外加上新的 `Volume`，从而模拟原程序的净额模式。
* 仅在 `CandleType` 订阅的蜡烛完成后评估信号，并要求 `IsFormedAndOnlineAndAllowTrading()` 允许交易。

### 出场条件
* 止损和止盈距离分别为 `Range × StopLossCoefficient` 与 `Range × TakeProfitCoefficient`。
* 持有多头时，若蜡烛最低价触及止损或最高价触及止盈，则以市价单平仓。
* 持有空头时，若蜡烛最高价碰到止损或最低价跌破止盈，同样以市价单平仓。
* 将任一系数设置为 0 可禁用对应的保护机制。

### 风险控制
* 分别为多头和空头维护日内计数器，在进入新交易日时归零。
* 交易数量由基类 `Strategy` 的 `Volume` 属性控制。
* 策略不挂出预设的止损/止盈订单，而是在满足条件时直接发送市价单完成退出。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `RangeMode` | 区间计算方式（`HighestLowest` 或 `CloseToClose`）。 | `HighestLowest` |
| `SlidingWindowDays` | 参与区间统计的日历天数。 | `3` |
| `StopLossCoefficient` | 与区间相乘得到止损距离的系数。 | `0.03` |
| `TakeProfitCoefficient` | 与区间相乘得到止盈距离的系数。 | `0.05` |
| `OffsetCoefficient` | 上下轨额外偏移的比例。 | `0.01` |
| `MaxPositionsPerDay` | 每个方向每日允许的最大开仓次数。 | `3` |
| `StartTime` | 当日重新计算区间的时间。 | `10:05` |
| `CandleType` | 用于计算和信号判定的蜡烛类型。 | 15 分钟蜡烛 |

## 实现细节
* 策略完全基于 StockSharp 高级功能（`SubscribeCandles`、`WhenNew`、市价单）实现，无需直接操作订单簿。
* 区间统计在策略内部完成，未使用任何 `GetValue` 调用，符合仓库的编码规范。
* 保护性止损/止盈通过监控蜡烛极值模拟，而非挂出真实的止损或限价单，便于在不同适配器之间迁移。
* 应要求未提供 Python 版本，本目录仅包含 C# 实现。
* 运行前需确保存在足够的历史蜡烛数据，以便首次计算区间时拥有完整的窗口。
